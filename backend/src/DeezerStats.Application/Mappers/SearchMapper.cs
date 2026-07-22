using DeezerStats.Application.DTOs.Search;

namespace DeezerStats.Application.Mappers
{
    /// <summary>
    /// Fournit des méthodes de mapping pour transformer les entités du catalogue en documents de recherche.
    /// </summary>
    public static class SearchMapper
    {
        /// <summary>
        /// Convertit un artiste enrichi en document de recherche plat.
        /// </summary>
        /// <param name="id">Identifiant de l'artiste.</param>
        /// <param name="name">Nom de l'artiste.</param>
        /// <param name="coverUrl">URL de la couverture (peut être nulle).</param>
        /// <returns>Un document de recherche pour l'artiste.</returns>
        public static SearchDocumentDto ToSearchDocument(Guid id, string name, string? coverUrl)
        {
            return new SearchDocumentDto
            {
                Id = id.ToString(),
                Type = "artist",
                Label = name,
                Subtitle = null,
                CoverUrl = coverUrl,
            };
        }

        /// <summary>
        /// Convertit un album enrichi en document de recherche plat.
        /// </summary>
        /// <param name="id">Identifiant de l'album.</param>
        /// <param name="title">Titre de l'album.</param>
        /// <param name="artistName">Nom de l'artiste associé à l'album.</param>
        /// <param name="coverUrl">URL de la couverture (peut être nulle).</param>
        /// <returns>Un document de recherche pour l'album.</returns>
        public static SearchDocumentDto ToSearchDocument(Guid id, string title, string artistName, string? coverUrl)
        {
            return new SearchDocumentDto
            {
                Id = id.ToString(),
                Type = "album",
                Label = title,
                Subtitle = artistName,
                CoverUrl = coverUrl,
            };
        }

        /// <summary>
        /// Convertit un morceau enrichi en document de recherche plat.
        /// </summary>
        /// <param name="id">Identifiant du morceau.</param>
        /// <param name="title">Titre du morceau.</param>
        /// <param name="artistName">Nom de l'artiste associé au morceau.</param>
        /// <param name="coverUrl">URL de la couverture (peut être nulle).</param>
        /// <returns>Un document de recherche pour le morceau.</returns>
        public static SearchDocumentDto ToSearchDocumentForTrack(Guid id, string title, string artistName, string? coverUrl)
        {
            return new SearchDocumentDto
            {
                Id = id.ToString(),
                Type = "track",
                Label = title,
                Subtitle = artistName,
                CoverUrl = coverUrl,
            };
        }
    }
}
