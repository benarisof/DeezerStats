using DeezerStats.Application.DTOs;
using DeezerStats.Application.UseCases.Import;
using Microsoft.AspNetCore.Mvc;

namespace DeezerStats.Api.Controllers;

/// <summary>
/// Point d'entrée pour l'import de l'historique d'écoute (fichier Excel mensuel exporté depuis
/// Deezer). Protégé par le FallbackPolicy global (voir Program.cs) : contrairement à AuthController,
/// aucun [AllowAnonymous] ici, un token JWT valide est requis.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ImportsController(IImportListeningHistoryUseCase importListeningHistoryUseCase) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ImportReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Import(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();

        await using Stream fileStream = file.OpenReadStream();

        ImportReport report = await importListeningHistoryUseCase.ExecuteAsync(
            new ImportListeningHistoryCommand(userId, fileStream),
            cancellationToken);

        return Ok(report);
    }
}
