using DeezerStats.Application.DTOs.Search;

namespace DeezerStats.Application.Ports.ExternalServices.Search
{
    public interface ISearchEnginePort
    {
        public Task<IEnumerable<SearchSuggestionDto>> GetSuggestionsAsync(string query, CancellationToken cancellationToken);

        public Task<SearchResultsPageDto> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken);

        public Task IndexDocumentAsync(SearchDocumentDto document, CancellationToken cancellationToken);
    }
}
