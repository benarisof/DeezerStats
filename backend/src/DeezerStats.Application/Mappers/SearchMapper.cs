using DeezerStats.Application.DTOs.Search;

namespace DeezerStats.Application.Mappers
{
    /// <summary>
    /// Fournit des méthodes de mapping pour transformer les entités du catalogue en documents de recherche.
    /// </summary>
    public static class SearchMapper
    {
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
