using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.UseCases.Stats.TopTracks;
using Microsoft.AspNetCore.Mvc;

namespace DeezerStats.Api.Controllers;

/// <summary>
/// Point d'entrée pour la consultation du classement des morceaux les plus écoutés. Protégé par
/// le FallbackPolicy global (voir Program.cs).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class TracksController(IGetTopTracksUseCase getTopTracksUseCase) : ApiControllerBase
{
    // Plafonné à 100 résultats, voir StatsRules.MaxRankedResults.
    [HttpGet("top")]
    [ProducesResponseType(typeof(PagedResult<TrackSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTopTracks(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        Guid userId = GetAuthenticatedUserId();

        PagedResult<TrackSummary> result = await getTopTracksUseCase.ExecuteAsync(
            new GetTopTracksQuery(userId, from, to, page, pageSize),
            cancellationToken);

        return Ok(result);
    }
}
