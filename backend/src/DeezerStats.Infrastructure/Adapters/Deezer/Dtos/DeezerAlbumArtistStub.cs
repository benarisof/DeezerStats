using System.Text.Json.Serialization;

namespace DeezerStats.Infrastructure.Adapters.Deezer.Dtos
{
    /// <summary>
    /// Sous-objet "artist" imbriqué dans une réponse de <c>GET /album/{id}</c> (voir
    /// DeezerAlbumDetailsResponse) : Deezer relie structurellement chaque album à son artiste, avec
    /// ses photos. Utilisé pour résoudre la couverture d'un artiste via un de ses albums déjà
    /// identifié plutôt que par une recherche texte sur son seul nom (voir
    /// DeezerHttpEnrichmentAdapter.FetchArtistMetadataAsync) -- bien plus fiable, puisque le lien
    /// album-artiste est une donnée structurée chez Deezer, pas une correspondance approximative.
    /// </summary>
    internal sealed class DeezerAlbumArtistStub
    {
        [JsonPropertyName("picture_medium")]
        public string? PictureMedium { get; set; }

        [JsonPropertyName("picture_big")]
        public string? PictureBig { get; set; }

        [JsonPropertyName("picture_xl")]
        public string? PictureXl { get; set; }
    }
}
