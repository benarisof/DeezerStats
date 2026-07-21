using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.UseCases.Stats;
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
    /// <summary>
    /// Classement paginé des artistes les plus écoutés (plafonné à 100 résultats, voir
    /// StatsRules.MaxRankedResults).
    /// </summary>
    /// <param name="from">Début de la plage de dates (incluse). Absent = depuis le début.</param>
    /// <param name="to">Fin de la plage de dates (incluse). Absent = jusqu'à aujourd'hui.</param>
    /// <param name="page">Numéro de page (défaut 1).</param>
    /// <param name="pageSize">Taille de page, 1 à 100 (défaut 20).</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>La page demandée du classement des artistes.</returns>
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

    /// <summary>
    /// Détail d'un artiste (page item) : agrégats d'écoute de l'utilisateur authentifié et
    /// morceaux triés par nombre d'écoutes décroissant.
    /// </summary>
    /// <param name="artistId">Identifiant de l'artiste.</param>
    /// <param name="from">Début de la plage de dates (incluse). Absent = depuis le début.</param>
    /// <param name="to">Fin de la plage de dates (incluse). Absent = jusqu'à aujourd'hui.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Le détail de l'artiste, ou 404 s'il n'existe pas.</returns>
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
