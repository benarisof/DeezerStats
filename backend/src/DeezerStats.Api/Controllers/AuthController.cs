using DeezerStats.Application.DTOs;
using DeezerStats.Application.UseCases.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeezerStats.Api.Controllers;

/// <summary>
/// Point d'entrée pour la gestion de l'authentification et des utilisateurs.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[AllowAnonymous] // Explicite le fait que ces routes ne nécessitent pas de token
public class AuthController(
    IRegisterUserUseCase registerUserUseCase,
    IAuthenticateUserUseCase authenticateUserUseCase) : ControllerBase
{
    /// <summary>
    /// Inscrit un nouvel utilisateur.
    /// </summary>
    /// <param name="command">Les données d'inscription de l'utilisateur.</param>
    /// <param name="cancellationToken">Jeton d'annulation propagé jusqu'à la base de données.</param>
    /// <returns>Une confirmation de la création du compte.</returns>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        // L'appel au Use Case déclenche en interne la validation FluentValidation
        // et lève une DomainException si l'email existe déjà.
        await registerUserUseCase.ExecuteAsync(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Authentifie un utilisateur et génère un jeton d'accès.
    /// </summary>
    /// <param name="command">Les identifiants de l'utilisateur.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Le jeton JWT (AccessToken).</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AccessTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] AuthenticateUserCommand command,
        CancellationToken cancellationToken)
    {
        AccessTokenDto token = await authenticateUserUseCase.ExecuteAsync(command, cancellationToken);
        return Ok(token);
    }
}
