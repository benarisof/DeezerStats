using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.UseCases.Stats;
using Microsoft.AspNetCore.Mvc;

namespace DeezerStats.Api.Controllers;

/// <summary>
/// Point d'entrée pour l'historique d'écoute (100 derniers morceaux écoutés, paginé). Protégé par
/// le FallbackPolicy global (voir Program.cs).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class HistoryController(IGetHistoryUseCase getHistoryUseCase) : ApiControllerBase
{
    /// <summary>
    /// Page d'historique des écoutes de l'utilisateur authentifié, triée par date d'écoute
    /// décroissante et plafonnée à 100 résultats (voir StatsRules.MaxRankedResults).
    /// </summary>
    /// <param name="from">Début de la plage de dates (incluse). Absent = depuis le début.</param>
    /// <param name="to">Fin de la plage de dates (incluse). Absent = jusqu'à aujourd'hui.</param>
    /// <param name="page">Numéro de page (défaut 1).</param>
    /// <param name="pageSize">Taille de page, 1 à 100 (défaut 20).</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>La page d'historique demandée.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<HistoryEntry>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        Guid userId = GetAuthenticatedUserId();

        PagedResult<HistoryEntry> result = await getHistoryUseCase.ExecuteAsync(
            new GetHistoryQuery(userId, from, to, page, pageSize),
            cancellationToken);

        return Ok(result);
    }
}
