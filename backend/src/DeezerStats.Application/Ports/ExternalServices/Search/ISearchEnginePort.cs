using DeezerStats.Application.DTOs.Search;

namespace DeezerStats.Application.Ports.ExternalServices.Search
{
    public interface ISearchEnginePort
    {
        /// <summary>
        /// Récupère les suggestions de recherche pour une requête donnée.
        /// </summary>
        /// <param name="query">La requête saisie par l'utilisateur.</param>
        /// <param name="cancellationToken">Jeton d'annulation de l'opération.</param>
        /// <returns>Une collection de suggestions de recherche.</returns>
        public Task<IEnumerable<SearchSuggestionDto>> GetSuggestionsAsync(string query, CancellationToken cancellationToken);

        /// <summary>
        /// Effectue une recherche paginée dans le catalogue.
        /// </summary>
        /// <param name="query">La requête de recherche.</param>
        /// <param name="page">Numéro de la page à récupérer (commence à 1).</param>
        /// <param name="pageSize">Nombre de résultats par page.</param>
        /// <param name="cancellationToken">Jeton d'annulation de l'opération.</param>
        /// <returns>Une page de résultats de recherche.</returns>
        public Task<SearchResultsPageDto> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>
        /// Indexe un lot de documents en un seul appel au moteur de recherche.
        /// À privilégier pour les imports massifs plutôt que des appels unitaires.
        /// </summary>
        /// <param name="documents">Collection des documents à indexer.</param>
        /// <param name="cancellationToken">Jeton d'annulation de l'opération.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public Task IndexDocumentsAsync(IEnumerable<SearchDocumentDto> documents, CancellationToken cancellationToken);
    }
}
