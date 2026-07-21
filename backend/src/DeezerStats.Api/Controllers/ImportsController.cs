using System.IdentityModel.Tokens.Jwt;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.UseCases.Imports;
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
public class ImportsController(IImportListeningHistoryUseCase importListeningHistoryUseCase) : ControllerBase
{
    /// <summary>
    /// Charge le fichier Excel mensuel d'historique d'écoute de l'utilisateur authentifié.
    /// </summary>
    /// <param name="file">Le classeur Excel (.xlsx) à importer, envoyé en multipart/form-data.</param>
    /// <param name="cancellationToken">Jeton d'annulation propagé jusqu'à la base de données.</param>
    /// <returns>Le rapport d'import (lignes importées, ignorées car déjà présentes, en erreur).</returns>
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

    private Guid GetAuthenticatedUserId()
    {
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        // Ne devrait jamais se produire derrière le FallbackPolicy (voir Program.cs) : un token JWT
        // valide contient toujours ce claim (voir JwtAccessTokenGenerator). Une InvalidOperationException
        // ici documente l'invariant plutôt que de laisser un Guid.Parse lever une FormatException opaque.
        if (!Guid.TryParse(subject, out Guid userId))
        {
            throw new InvalidOperationException(
                "Le token JWT ne contient pas d'identifiant utilisateur valide.");
        }

        return userId;
    }
}
