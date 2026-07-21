using System.Text.Json.Serialization;

namespace DeezerStats.Infrastructure.Adapters.Deezer.Dtos
{
    /// <summary>
    /// Élément de la liste "data" d'une réponse de recherche d'album (voir
    /// DeezerAlbumSearchResponse). Seul l'identifiant nous intéresse : il sert à récupérer les
    /// détails complets via <c>GET /album/{id}</c> (voir DeezerAlbumDetailsResponse).
    /// </summary>
    internal sealed class DeezerAlbumSearchResult
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    }
}
