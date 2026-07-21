using System.Text.Json.Serialization;

namespace DeezerStats.Infrastructure.Adapters.Deezer.Dtos
{
    /// <summary>
    /// Deezer renvoie un corps d'erreur avec un statut HTTP 200 lorsqu'une ressource est
    /// introuvable (ex. ISRC inconnu) : ce champ, présent sur toutes les réponses de l'API, permet
    /// de le détecter (voir DeezerHttpEnrichmentAdapter).
    /// </summary>
    internal sealed class DeezerError
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }
    }
}
