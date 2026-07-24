using System.Net.Http.Json;
using System.Text.Json;
using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.Adapters.Deezer.Dtos;
using Polly.Timeout;

namespace DeezerStats.Infrastructure.Adapters.Deezer
{
    /// <summary>
    /// Adaptateur HTTP vers l'API publique Deezer (aucune clé requise pour les endpoints utilisés
    /// ici). Volontairement tolérant aux pannes : ressource introuvable, erreur Deezer, JSON invalide
    /// ou échec réseau/timeout renvoient null plutôt que de lever une exception, pour ne jamais faire
    /// échouer l'import ou la consultation d'un artiste/album/morceau à cause d'un enrichissement raté.
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

        // Résout la couverture en priorité via un album déjà connu (lien album -> artiste fiable
        // chez Deezer), et seulement en repli via une recherche par nom seul (ambiguë, voir
        // FetchArtistByNameAsync).
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

        // Distingue les échecs "attendus" (réseau, JSON malformé, timeout) -- à absorber en
        // renvoyant null -- d'une annulation demandée par l'appelant, qui doit continuer à se propager.
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

        // Recherche album + détails, factorisée entre FetchAlbumMetadataAsync (couverture/date/durée
        // de l'album) et FetchArtistMetadataAsync (couverture artiste via le sous-objet "artist").
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

        // Repli : recherche par nom seul, intrinsèquement ambiguë (homonymes) -- le résultat n'est
        // accepté que si le nom retourné correspond, mieux vaut aucune couverture qu'une couverture
        // du mauvais artiste.
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
