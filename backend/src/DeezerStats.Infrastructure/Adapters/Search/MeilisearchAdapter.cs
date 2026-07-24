using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.Ports.ExternalServices.Search;
using Meilisearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeezerStats.Infrastructure.Adapters.Search;

public class MeilisearchAdapter(
    MeilisearchClient client,
    IOptions<MeilisearchOptions> options,
    ILogger<MeilisearchAdapter> logger) : ISearchEnginePort
{
    private static readonly Action<ILogger, string, Exception?> _logSuggestionsError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2001, "MeilisearchSuggestionsError"),
            "Erreur Meilisearch lors de la récupération des suggestions pour la requête: {Query}");

    private static readonly Action<ILogger, string, Exception?> _logSearchError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2002, "MeilisearchSearchError"),
            "Erreur Meilisearch lors de la recherche du catalogue pour la requête: {Query}");

    private static readonly Action<ILogger, int, Exception?> _logIndexError =
        LoggerMessage.Define<int>(
            LogLevel.Error,
            new EventId(2003, "MeilisearchIndexError"),
            "Erreur lors de l'indexation de {DocumentCount} document(s) dans Meilisearch.");

    private static readonly Action<ILogger, int, Exception?> _logIndexDebug =
        LoggerMessage.Define<int>(
            LogLevel.Debug,
            new EventId(2004, "MeilisearchIndexDebug"),
            "{DocumentCount} document(s) poussé(s) dans Meilisearch avec succès.");

    private readonly string _indexName = options.Value.IndexName;

    // Dégrade silencieusement sur une panne Meilisearch (liste vide) plutôt que de propager l'erreur,
    // contrairement à SearchAsync : cet appel se déclenche à chaque frappe (autocomplétion),
    // une indisponibilité momentanée ne doit jamais interrompre la saisie avec une erreur bruyante.
    public async Task<IEnumerable<SearchSuggestionDto>> GetSuggestionsAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            Meilisearch.Index index = client.Index(_indexName);

            var searchQuery = new SearchQuery
            {
                Limit = 10,
                AttributesToRetrieve = ["id", "type", "label", "subtitle", "coverUrl"],
            };

            ISearchable<SearchSuggestionDto> result = await index.SearchAsync<SearchSuggestionDto>(query, searchQuery, cancellationToken);
            return result.Hits;
        }
        catch (MeilisearchApiError ex)
        {
            _logSuggestionsError(logger, query, ex);
            return [];
        }
    }

    // Propage la panne Meilisearch (500) plutôt que de la masquer en page de résultats vide :
    // contrairement à GetSuggestionsAsync, cet appel répond à une action explicite de l'utilisateur,
    // renvoyer silencieusement "0 résultat" laisserait croire à tort que la recherche n'a rien trouvé.
    public async Task<SearchResultsPageDto> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken)
    {
        try
        {
            Meilisearch.Index index = client.Index(_indexName);

            var searchQuery = new SearchQuery
            {
                HitsPerPage = pageSize,
                Page = page,
                AttributesToRetrieve = ["id", "type", "label", "subtitle", "coverUrl"],
            };

            ISearchable<SearchSuggestionDto> result = await index.SearchAsync<SearchSuggestionDto>(query, searchQuery, cancellationToken);

            // Cast sécurisé vers PaginatedSearchResult pour accéder aux compteurs
            if (result is PaginatedSearchResult<SearchSuggestionDto> paginatedResult)
            {
                return new SearchResultsPageDto
                {
                    Items = paginatedResult.Hits,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = paginatedResult.TotalHits,
                    TotalPages = paginatedResult.TotalPages,
                };
            }

            // Fallback de sécurité
            return new SearchResultsPageDto
            {
                Items = result.Hits,
                Page = page,
                PageSize = pageSize,
                TotalItems = result.Hits.Count,
                TotalPages = 1,
            };
        }
        catch (MeilisearchApiError ex)
        {
            _logSearchError(logger, query, ex);
            throw;
        }
    }

    public async Task IndexDocumentsAsync(IEnumerable<SearchDocumentDto> documents, CancellationToken cancellationToken)
    {
        List<SearchDocumentDto> documentList = documents as List<SearchDocumentDto> ?? [.. documents];

        if (documentList.Count == 0)
        {
            return;
        }

        try
        {
            Meilisearch.Index index = client.Index(_indexName);
            await index.AddDocumentsAsync(documentList, primaryKey: "id", cancellationToken: cancellationToken);

            _logIndexDebug(logger, documentList.Count, null);
        }
        catch (MeilisearchApiError ex)
        {
            _logIndexError(logger, documentList.Count, ex);
            throw;
        }
    }
}
