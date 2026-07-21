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

                if (details is null || details.Error is not null)
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
            catch (Exception ex) when (IsTransientEnrichmentFailure(ex))
            {
                return null;
            }
        }

        public async Task<DeezerArtistMetadata?> FetchArtistMetadataAsync(string artistName, CancellationToken ct = default)
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
                var coverUrl = result.PictureXl ?? result.PictureBig ?? result.PictureMedium;

                return new DeezerArtistMetadata(coverUrl);
            }
            catch (Exception ex) when (IsTransientEnrichmentFailure(ex))
            {
                return null;
            }
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
    }
}
