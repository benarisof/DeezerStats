using System.Collections.Concurrent;
using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.Ports.ExternalServices.Search;

namespace DeezerStats.Api.IntegrationTests;

/// <summary>
/// Remplace le véritable MeilisearchAdapter dans les tests d'intégration (voir
/// CustomWebApplicationFactory) : le pipeline CI n'a pas d'instance Meilisearch (contrairement à
/// docker-compose en local), et un test de recherche ne doit de toute façon pas dépendre de la
/// latence d'indexation asynchrone réelle de Meilisearch. Stocke les documents en mémoire (upsert
/// par Id, comme le ferait réellement Meilisearch) et retrouve les correspondances par sous-chaîne
/// insensible à la casse -- suffisant pour prouver que l'indexation à l'import et la ré-indexation
/// à l'enrichissement sont bien déclenchées. La tolérance aux fautes de frappe et la pertinence du
/// classement restent hors-scope (voir ticket 10.4, qui nécessite un vrai Meilisearch).
/// </summary>
public class FakeSearchEnginePort : ISearchEnginePort
{
    private readonly ConcurrentDictionary<string, SearchDocumentDto> _documents = new();

    public Task<IEnumerable<SearchSuggestionDto>> GetSuggestionsAsync(string query, CancellationToken cancellationToken) =>
        Task.FromResult(Match(query).Take(10));

    public Task<SearchResultsPageDto> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var matches = Match(query).ToList();
        List<SearchSuggestionDto> pageItems = [.. matches.Skip((page - 1) * pageSize).Take(pageSize)];

        return Task.FromResult(new SearchResultsPageDto
        {
            Items = pageItems,
            Page = page,
            PageSize = pageSize,
            TotalItems = matches.Count,
            TotalPages = (int)Math.Ceiling(matches.Count / (double)pageSize),
        });
    }

    public Task IndexDocumentsAsync(IEnumerable<SearchDocumentDto> documents, CancellationToken cancellationToken)
    {
        foreach (SearchDocumentDto document in documents)
        {
            _documents[document.Id] = document;
        }

        return Task.CompletedTask;
    }

    private IEnumerable<SearchSuggestionDto> Match(string query) =>
        _documents.Values
            .Where(d => d.Label.Contains(query, StringComparison.OrdinalIgnoreCase)
                || (d.Subtitle?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(d => new SearchSuggestionDto
            {
                Id = Guid.Parse(d.Id),
                Type = d.Type,
                Label = d.Label,
                Subtitle = d.Subtitle,
                CoverUrl = d.CoverUrl,
            });
}
