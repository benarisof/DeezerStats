namespace DeezerStats.Application.DTOs.Search
{
    /// <summary>
    /// DTO utilisé spécifiquement pour l'indexation dans le moteur de recherche (Write model).
    /// Il possède la même structure plate que la suggestion pour optimiser les performances.
    /// </summary>
    public class SearchDocumentDto
    {
        public string Id { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string? Subtitle { get; set; }

        public string? CoverUrl { get; set; }
    }
}
