using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.Ports.ExternalServices.Search;

namespace DeezerStats.Application.UseCases.Search
{
    public class GetSearchSuggestionsUseCase(ISearchEnginePort searchEnginePort) : IGetSearchSuggestionsUseCase
    {
        private readonly ISearchEnginePort _searchEnginePort = searchEnginePort;

        public async Task<IEnumerable<SearchSuggestionDto>> ExecuteAsync(string query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 4)
            {
                // Retourne une liste vide si la condition métier n'est pas remplie
                return [];
            }

            return await _searchEnginePort.GetSuggestionsAsync(query.Trim(), cancellationToken);
        }
    }
}
