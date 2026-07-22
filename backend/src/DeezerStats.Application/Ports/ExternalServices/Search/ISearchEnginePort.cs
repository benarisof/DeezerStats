using DeezerStats.Application.DTOs.Search;

namespace DeezerStats.Application.Ports.ExternalServices.Search
{
    public interface ISearchEnginePort
    {
        public Task<IEnumerable<SearchSuggestionDto>> GetSuggestionsAsync(string query, CancellationToken cancellationToken);

        public Task<SearchResultsPageDto> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>
        /// Indexe un lot de documents en un seul appel au moteur de recherche (voir
        /// MeilisearchAdapter) : à privilégier systématiquement sur plusieurs appels unitaires,
        /// notamment lors d'un import qui peut créer des milliers d'entités d'un coup.
        /// </summary>
        public Task IndexDocumentsAsync(IEnumerable<SearchDocumentDto> documents, CancellationToken cancellationToken);
    }
}
