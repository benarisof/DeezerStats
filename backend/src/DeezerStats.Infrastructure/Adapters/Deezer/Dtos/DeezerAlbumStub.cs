using System.Text.Json.Serialization;

namespace DeezerStats.Infrastructure.Adapters.Deezer.Dtos
{
    /// <summary>
    /// Sous-objet "album" imbriqué dans une réponse de <c>GET /track/isrc:{isrc}</c> : ne contient
    /// que les pochettes, pas la date de sortie (voir DeezerAlbumDetailsResponse pour l'objet album
    /// complet, obtenu via <c>GET /album/{id}</c>).
    /// </summary>
    internal sealed class DeezerAlbumStub
    {
        [JsonPropertyName("cover_medium")]
        public string? CoverMedium { get; set; }

        [JsonPropertyName("cover_big")]
        public string? CoverBig { get; set; }

        [JsonPropertyName("cover_xl")]
        public string? CoverXl { get; set; }
    }
}
