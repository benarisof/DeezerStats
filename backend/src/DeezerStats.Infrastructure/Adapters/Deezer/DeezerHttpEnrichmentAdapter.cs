using System.Net.Http.Json;
using System.Text.Json;
using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.Adapters.Deezer.Dtos;
using Polly.Timeout;

namespace DeezerStats.Infrastructure.Adapters.Deezer
{
    /// <summary>
    /// Adaptateur HTTP vers l'API publique Deezer (https://developers.deezer.com/api), qui ne
    /// nécessite aucune clé pour les endpoints de lecture utilisés ici (track/isrc:, search/album,
    /// album/{id}, search/artist). La résilience (retry + timeout) est configurée au niveau du
    /// HttpClient typé, pas ici (voir Infrastructure.DependencyInjection,
    /// AddStandardResilienceHandler) : cet adaptateur ne fait que traduire les réponses JSON de
    /// Deezer en objets du vocabulaire applicatif.
    ///
    /// Volontairement tolérant aux pannes : une ressource introuvable, une réponse d'erreur Deezer,
    /// un JSON invalide ou un échec réseau/timeout renvoient null plutôt que de lever une
    /// exception. Un échec d'enrichissement ne doit jamais faire échouer l'import ou la
    /// consultation de l'artiste/l'album/le morceau concerné (voir GetOrEnrichArtistUseCase/
    /// GetOrEnrichAlbumUseCase/GetOrEnrichTrackUseCase) : il laisse simplement ses métadonnées non
    /// enrichies, en vue d'une prochaine tentative.
    /// </summary>
    public class DeezerHttpEnrichmentAdapter(HttpClient httpClient) : IDeezerEnrichmentPort
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient = httpClient;

        public async Task<DeezerTrackMetadata?> FetchTrackMetadataAsync(Isrc isrc, CancellationToken ct = default)
        {
            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync($"track/isrc:{isrc.Value}", ct);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                DeezerTrackResponse? payload = await response.Content
                    .ReadFromJsonAsync<DeezerTrackResponse>(_jsonOptions, ct);

                if (payload is null || payload.Error is not null || payload.Duration is null)
                {
                    return null;
                }

                var coverUrl = payload.Album?.CoverXl ?? payload.Album?.CoverBig ?? payload.Album?.CoverMedium;

                return new DeezerTrackMetadata(coverUrl, new Duration(payload.Duration.Value));
            }
            catch (Exception ex) when (IsTransientEnrichmentFailure(ex))
            {
                return null;
            }
        }

        public async Task<DeezerAlbumMetadata?> FetchAlbumMetadataAsync(string albumTitle, string artistName, CancellationToken ct = default)
        {
            DeezerAlbumDetailsResponse? details = await FetchAlbumDetailsAsync(artistName, albumTitle, ct);

            if (details is null)
            {
                return null;
            }

            var coverUrl = details.CoverXl ?? details.CoverBig ?? details.CoverMedium;
            DateOnly? releaseDate = DateOnly.TryParse(details.ReleaseDate, out DateOnly parsedDate)
                ? parsedDate
                : null;
            Duration? duration = details.Duration is int seconds ? new Duration(seconds) : null;

            return new DeezerAlbumMetadata(coverUrl, releaseDate, duration);
        }

        /// <summary>
        /// Résout la couverture d'un artiste en priorité via un de ses albums déjà connus (le lien
        /// album → artiste est une donnée structurée chez Deezer, donc fiable), et seulement en
        /// repli via une recherche texte sur son seul nom (ambiguë : sujette aux homonymes -- voir
        /// FetchArtistByNameAsync, qui n'accepte le résultat que si le nom retourné correspond).
        /// </summary>
        /// <param name="artistName">Nom de l'artiste, utilisé pour la recherche album et en repli pour la recherche par nom.</param>
        /// <param name="knownAlbumTitle">Titre d'un album déjà connu de cet artiste, ou null si aucun n'est disponible.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Les métadonnées de l'artiste (couverture), ou null si aucune n'a pu être résolue de façon fiable.</returns>
        public async Task<DeezerArtistMetadata?> FetchArtistMetadataAsync(string artistName, string? knownAlbumTitle, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(knownAlbumTitle))
            {
                DeezerAlbumDetailsResponse? albumDetails = await FetchAlbumDetailsAsync(artistName, knownAlbumTitle, ct);
                var coverViaAlbum = albumDetails?.Artist?.PictureXl
                    ?? albumDetails?.Artist?.PictureBig
                    ?? albumDetails?.Artist?.PictureMedium;

                if (!string.IsNullOrWhiteSpace(coverViaAlbum))
                {
                    return new DeezerArtistMetadata(coverViaAlbum);
                }
            }

            return await FetchArtistByNameAsync(artistName, ct);
        }

        /// <summary>
        /// Distingue les échecs "attendus" d'appel à Deezer (réseau, JSON malformé, timeout déclenché
        /// par la politique de résilience) — à absorber en renvoyant null — d'une annulation
        /// explicitement demandée par l'appelant (son <see cref="CancellationToken"/>), qui doit au
        /// contraire continuer à se propager normalement.
        /// </summary>
        private static bool IsTransientEnrichmentFailure(Exception ex) => ex switch
        {
            HttpRequestException => true,
            JsonException => true,
            TimeoutRejectedException => true,
            OperationCanceledException => false,
            _ => false,
        };

        private static bool NamesMatch(string left, string right) =>
            string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Recherche album + détails (<c>GET search/album</c> puis <c>GET album/{id}</c>), factorisée
        /// entre FetchAlbumMetadataAsync (couverture/date de sortie/durée de l'album lui-même) et
        /// FetchArtistMetadataAsync (couverture de l'artiste via le sous-objet "artist" de la même
        /// réponse de détails).
        /// </summary>
        private async Task<DeezerAlbumDetailsResponse?> FetchAlbumDetailsAsync(string artistName, string albumTitle, CancellationToken ct)
        {
            try
            {
                var query = $"artist:\"{artistName}\" album:\"{albumTitle}\"";
                using HttpResponseMessage searchResponse = await _httpClient.GetAsync(
                    $"search/album?q={Uri.EscapeDataString(query)}", ct);

                if (!searchResponse.IsSuccessStatusCode)
                {
                    return null;
                }

                DeezerAlbumSearchResponse? searchPayload = await searchResponse.Content
                    .ReadFromJsonAsync<DeezerAlbumSearchResponse>(_jsonOptions, ct);

                if (searchPayload is null || searchPayload.Error is not null || searchPayload.Data is not { Count: > 0 })
                {
                    return null;
                }

                var albumId = searchPayload.Data[0].Id;

                using HttpResponseMessage detailsResponse = await _httpClient.GetAsync($"album/{albumId}", ct);

                if (!detailsResponse.IsSuccessStatusCode)
                {
                    return null;
                }

                DeezerAlbumDetailsResponse? details = await detailsResponse.Content
                    .ReadFromJsonAsync<DeezerAlbumDetailsResponse>(_jsonOptions, ct);

                return details is null || details.Error is not null ? null : details;
            }
            catch (Exception ex) when (IsTransientEnrichmentFailure(ex))
            {
                return null;
            }
        }

        /// <summary>
        /// Repli : recherche par nom seul (<c>GET search/artist</c>). Une recherche texte libre sur
        /// un seul champ est intrinsèquement ambiguë (homonymes, classement par pertinence Deezer et
        /// non par exactitude) : le résultat n'est accepté que si le nom qu'il porte correspond,
        /// une fois normalisé, au nom recherché -- mieux vaut aucune couverture qu'une couverture du
        /// mauvais artiste.
        /// </summary>
        private async Task<DeezerArtistMetadata?> FetchArtistByNameAsync(string artistName, CancellationToken ct)
        {
            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync(
                    $"search/artist?q={Uri.EscapeDataString(artistName)}", ct);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                DeezerArtistSearchResponse? payload = await response.Content
                    .ReadFromJsonAsync<DeezerArtistSearchResponse>(_jsonOptions, ct);

                if (payload is null || payload.Error is not null || payload.Data is not { Count: > 0 })
                {
                    return null;
                }

                DeezerArtistSearchResult result = payload.Data[0];

                if (result.Name is null || !NamesMatch(result.Name, artistName))
                {
                    return null;
                }

                var coverUrl = result.PictureXl ?? result.PictureBig ?? result.PictureMedium;

                return new DeezerArtistMetadata(coverUrl);
            }
            catch (Exception ex) when (IsTransientEnrichmentFailure(ex))
            {
                return null;
            }
        }
    }
}
