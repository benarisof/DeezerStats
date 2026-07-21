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
    /// <summary>
    /// Classement paginé des albums les plus écoutés (plafonné à 100 résultats, voir
    /// StatsRules.MaxRankedResults).
    /// </summary>
    /// <param name="from">Début de la plage de dates (incluse). Absent = depuis le début.</param>
    /// <param name="to">Fin de la plage de dates (incluse). Absent = jusqu'à aujourd'hui.</param>
    /// <param name="page">Numéro de page (défaut 1).</param>
    /// <param name="pageSize">Taille de page, 1 à 100 (défaut 20).</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>La page demandée du classement des albums.</returns>
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

    /// <summary>
    /// Détail d'un album (page item) : agrégats d'écoute de l'utilisateur authentifié et morceaux
    /// triés par nombre d'écoutes décroissant.
    /// </summary>
    /// <param name="albumId">Identifiant de l'album.</param>
    /// <param name="from">Début de la plage de dates (incluse). Absent = depuis le début.</param>
    /// <param name="to">Fin de la plage de dates (incluse). Absent = jusqu'à aujourd'hui.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Le détail de l'album, ou 404 s'il n'existe pas.</returns>
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
