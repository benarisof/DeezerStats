using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.Ports.ExternalServices.Search;

namespace DeezerStats.Application.UseCases.Search
{
    public class SearchCatalogUseCase(ISearchEnginePort searchEnginePort) : ISearchCatalogUseCase
    {
        private readonly ISearchEnginePort _searchEnginePort = searchEnginePort;

        public async Task<SearchResultsPageDto> ExecuteAsync(string query, int page, int pageSize, CancellationToken cancellationToken)
        {
            // Sécurisation basique de la pagination
            var currentPage = page < 1 ? 1 : page;
            var currentPageSize = pageSize < 1 ? 20 : pageSize;

            if (string.IsNullOrWhiteSpace(query))
            {
                return new SearchResultsPageDto
                {
                    Items = [],
                    Page = currentPage,
                    PageSize = currentPageSize,
                    TotalItems = 0,
                    TotalPages = 0,
                };
            }

            return await _searchEnginePort.SearchAsync(query.Trim(), currentPage, currentPageSize, cancellationToken);
        }
    }
}
