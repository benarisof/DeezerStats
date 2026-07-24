using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.UseCases.Stats.Artist;
using DeezerStats.Application.UseCases.Stats.TopArtists;
using Microsoft.AspNetCore.Mvc;

namespace DeezerStats.Api.Controllers;

/// <summary>
/// Point d'entrée pour la consultation des artistes : classement des plus écoutés et page de
/// détail d'un artiste. Protégé par le FallbackPolicy global (voir Program.cs).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ArtistsController(
    IGetTopArtistsUseCase getTopArtistsUseCase,
    IGetArtistDetailUseCase getArtistDetailUseCase) : ApiControllerBase
{
    // Plafonné à 100 résultats, voir StatsRules.MaxRankedResults.
    [HttpGet("top")]
    [ProducesResponseType(typeof(PagedResult<ArtistSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTopArtists(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        Guid userId = GetAuthenticatedUserId();

        PagedResult<ArtistSummary> result = await getTopArtistsUseCase.ExecuteAsync(
            new GetTopArtistsQuery(userId, from, to, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{artistId:guid}")]
    [ProducesResponseType(typeof(ArtistDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetArtistDetail(
        Guid artistId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();

        ArtistDetail? detail = await getArtistDetailUseCase.ExecuteAsync(
            new GetArtistDetailQuery(userId, artistId, from, to),
            cancellationToken);

        return detail is null ? NotFound() : Ok(detail);
    }
}
