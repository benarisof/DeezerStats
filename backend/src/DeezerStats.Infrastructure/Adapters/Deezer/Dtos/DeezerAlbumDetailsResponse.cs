using System.Text.Json.Serialization;

namespace DeezerStats.Infrastructure.Adapters.Deezer.Dtos
{
    /// <summary>
    /// DTO interne de désérialisation de la réponse JSON de l'endpoint Deezer
    /// <c>GET /album/{id}</c> : contrairement au sous-objet imbriqué dans une réponse de morceau
    /// (voir DeezerAlbumStub), celle-ci contient la date de sortie et la durée totale de l'album.
    /// </summary>
    internal sealed class DeezerAlbumDetailsResponse
    {
        [JsonPropertyName("error")]
        public DeezerError? Error { get; set; }

        [JsonPropertyName("cover_medium")]
        public string? CoverMedium { get; set; }

        [JsonPropertyName("cover_big")]
        public string? CoverBig { get; set; }

        [JsonPropertyName("cover_xl")]
        public string? CoverXl { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("duration")]
        public int? Duration { get; set; }

        [JsonPropertyName("artist")]
        public DeezerAlbumArtistStub? Artist { get; set; }
    }
}
