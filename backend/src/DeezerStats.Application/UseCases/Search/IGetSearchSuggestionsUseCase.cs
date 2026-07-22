using DeezerStats.Application.DTOs.Search;

namespace DeezerStats.Application.UseCases.Search
{
    public interface IGetSearchSuggestionsUseCase
    {
        public Task<IEnumerable<SearchSuggestionDto>> ExecuteAsync(string query, CancellationToken cancellationToken);
    }
}
