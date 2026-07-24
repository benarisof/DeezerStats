using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.UseCases.Stats.History;
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
    // Triée par date d'écoute décroissante, plafonnée à 100 résultats (voir StatsRules.MaxRankedResults).
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
