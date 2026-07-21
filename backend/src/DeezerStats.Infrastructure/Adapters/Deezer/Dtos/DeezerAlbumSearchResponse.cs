using System.Text.Json.Serialization;

namespace DeezerStats.Infrastructure.Adapters.Deezer.Dtos
{
    /// <summary>
    /// DTO interne de désérialisation de la réponse JSON de l'endpoint Deezer
    /// <c>GET /search/album?q=...</c>, utilisé pour résoudre l'identifiant Deezer d'un album à
    /// partir de son titre et du nom de son artiste (voir DeezerHttpEnrichmentAdapter).
    /// </summary>
    internal sealed class DeezerAlbumSearchResponse
    {
        [JsonPropertyName("error")]
        public DeezerError? Error { get; set; }

        [JsonPropertyName("data")]
        public List<DeezerAlbumSearchResult>? Data { get; set; }
    }
}
