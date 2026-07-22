namespace DeezerStats.Application.DTOs.Search
{
    public class SearchResultsPageDto
    {
        public IEnumerable<SearchSuggestionDto> Items { get; set; } = [];

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalItems { get; set; }

        public int TotalPages { get; set; }
    }
}
