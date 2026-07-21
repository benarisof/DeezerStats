using System.Text.Json.Serialization;

namespace DeezerStats.Infrastructure.Adapters.Deezer.Dtos
{
    /// <summary>
    /// DTO interne de désérialisation de la réponse JSON de l'endpoint Deezer
    /// <c>GET /track/isrc:{isrc}</c>. Volontairement séparé des types du domaine applicatif
    /// (voir DeezerTrackMetadata) : ce sont deux modèles différents dont le couplage ne doit pas
    /// fuiter au-delà de DeezerHttpEnrichmentAdapter.
    /// </summary>
    internal sealed class DeezerTrackResponse
    {
        [JsonPropertyName("error")]
        public DeezerError? Error { get; set; }

        [JsonPropertyName("duration")]
        public int? Duration { get; set; }

        [JsonPropertyName("album")]
        public DeezerAlbumStub? Album { get; set; }
    }
}
