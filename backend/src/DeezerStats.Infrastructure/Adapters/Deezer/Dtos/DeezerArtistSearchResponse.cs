using System.Text.Json.Serialization;

namespace DeezerStats.Infrastructure.Adapters.Deezer.Dtos
{
    /// <summary>
    /// DTO interne de désérialisation de la réponse JSON de l'endpoint Deezer
    /// <c>GET /search/artist?q=...</c>, utilisé pour résoudre la photo d'un artiste à partir de son
    /// nom (voir DeezerHttpEnrichmentAdapter). Contrairement à la recherche d'album, les résultats
    /// de recherche d'artiste contiennent déjà les photos : aucun second appel de type "détails"
    /// n'est nécessaire ici.
    /// </summary>
    internal sealed class DeezerArtistSearchResponse
    {
        [JsonPropertyName("error")]
        public DeezerError? Error { get; set; }

        [JsonPropertyName("data")]
        public List<DeezerArtistSearchResult>? Data { get; set; }
    }
}
