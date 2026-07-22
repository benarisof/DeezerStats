using DeezerStats.Application.DTOs.Search;

namespace DeezerStats.Application.UseCases.Search
{
    public interface ISearchCatalogUseCase
    {
        public Task<SearchResultsPageDto> ExecuteAsync(string query, int page, int pageSize, CancellationToken cancellationToken);
    }
}
