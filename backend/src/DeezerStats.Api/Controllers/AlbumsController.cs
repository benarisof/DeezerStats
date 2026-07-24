using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.UseCases.Stats.Album;
using DeezerStats.Application.UseCases.Stats.TopAlbums;
using Microsoft.AspNetCore.Mvc;

namespace DeezerStats.Api.Controllers;

/// <summary>
/// Point d'entrée pour la consultation des albums : classement des plus écoutés et page de détail
/// d'un album. Protégé par le FallbackPolicy global (voir Program.cs).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AlbumsController(
    IGetTopAlbumsUseCase getTopAlbumsUseCase,
    IGetAlbumDetailUseCase getAlbumDetailUseCase) : ApiControllerBase
{
    // Plafonné à 100 résultats, voir StatsRules.MaxRankedResults.
    [HttpGet("top")]
    [ProducesResponseType(typeof(PagedResult<AlbumSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTopAlbums(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        Guid userId = GetAuthenticatedUserId();

        PagedResult<AlbumSummary> result = await getTopAlbumsUseCase.ExecuteAsync(
            new GetTopAlbumsQuery(userId, from, to, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{albumId:guid}")]
    [ProducesResponseType(typeof(AlbumDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAlbumDetail(
        Guid albumId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();

        AlbumDetail? detail = await getAlbumDetailUseCase.ExecuteAsync(
            new GetAlbumDetailQuery(userId, albumId, from, to),
            cancellationToken);

        return detail is null ? NotFound() : Ok(detail);
    }
}
