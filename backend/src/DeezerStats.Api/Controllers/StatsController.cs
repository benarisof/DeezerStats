using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.UseCases.Stats.Home;
using Microsoft.AspNetCore.Mvc;

namespace DeezerStats.Api.Controllers;

/// <summary>
/// Point d'entrée pour les statistiques agrégées de la page d'accueil (top 10 albums/artistes/
/// morceaux). Protégé par le FallbackPolicy global (voir Program.cs).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class StatsController(IGetHomeStatsUseCase getHomeStatsUseCase) : ApiControllerBase
{
    [HttpGet("home")]
    [ProducesResponseType(typeof(HomeStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHomeStats(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();

        HomeStatsResponse response = await getHomeStatsUseCase.ExecuteAsync(
            new GetHomeStatsQuery(userId, from, to),
            cancellationToken);

        return Ok(response);
    }
}
