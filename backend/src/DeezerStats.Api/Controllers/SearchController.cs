using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.UseCases.Search;
using Microsoft.AspNetCore.Mvc;

namespace DeezerStats.Api.Controllers;

/// <summary>
/// Point d'entrée pour la recherche dans le catalogue (suggestions d'autocomplétion et recherche
/// complète via Meilisearch). Protégé par le FallbackPolicy global (voir Program.cs).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SearchController(
    IGetSearchSuggestionsUseCase getSearchSuggestionsUseCase,
    ISearchCatalogUseCase searchCatalogUseCase) : ApiControllerBase
{
    /// <summary>
    /// Suggestions d'autocomplétion (déclenchées côté front à partir de 4 caractères).
    /// </summary>
    /// <param name="q">Termes recherchés (au moins 4 caractères).</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>La liste des suggestions (albums / artistes / morceaux).</returns>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(IEnumerable<SearchSuggestionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSuggestions(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        IEnumerable<SearchSuggestionDto> suggestions = await getSearchSuggestionsUseCase.ExecuteAsync(q, cancellationToken);
        return Ok(suggestions);
    }

    /// <summary>
    /// Recherche complète dans le catalogue (clic sur une suggestion ou touche Entrée), paginée.
    /// </summary>
    /// <param name="q">Termes recherchés.</param>
    /// <param name="page">Numéro de page (défaut 1).</param>
    /// <param name="pageSize">Taille de page (défaut 20).</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>La page de résultats demandée.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResultsPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        SearchResultsPageDto result = await searchCatalogUseCase.ExecuteAsync(q, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
