namespace DeezerStats.Application.DTOs.Search
{
    public class SearchSuggestionDto
    {
        public Guid Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string? Subtitle { get; set; }

        public string? CoverUrl { get; set; }
    }
}
