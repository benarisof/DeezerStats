using System.Text.Json.Serialization;

namespace DeezerStats.Infrastructure.Adapters.Deezer.Dtos
{
    /// <summary>
    /// Élément de la liste "data" d'une réponse de recherche d'artiste (voir
    /// DeezerArtistSearchResponse).
    /// </summary>
    internal sealed class DeezerArtistSearchResult
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture_medium")]
        public string? PictureMedium { get; set; }

        [JsonPropertyName("picture_big")]
        public string? PictureBig { get; set; }

        [JsonPropertyName("picture_xl")]
        public string? PictureXl { get; set; }
    }
}
