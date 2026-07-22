using DeezerStats.Application.DTOs;
using DeezerStats.Application.UseCases.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeezerStats.Api.Controllers;

/// <summary>
/// Point d'entrée pour la gestion de l'authentification et des utilisateurs. Register/Login/Refresh
/// sont explicitement anonymes (le client ne dispose pas encore, ou plus, d'un access token valide) ;
/// Logout et Me héritent du FallbackPolicy global (voir Program.cs) et exigent un access token.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController(
    IRegisterUserUseCase registerUserUseCase,
    IAuthenticateUserUseCase authenticateUserUseCase,
    IRefreshAccessTokenUseCase refreshAccessTokenUseCase,
    ILogoutUserUseCase logoutUserUseCase,
    IGetCurrentUserUseCase getCurrentUserUseCase) : ApiControllerBase
{
    /// <summary>
    /// Inscrit un nouvel utilisateur et le connecte automatiquement.
    /// </summary>
    /// <param name="command">Les données d'inscription de l'utilisateur.</param>
    /// <param name="cancellationToken">Jeton d'annulation propagé jusqu'à la base de données.</param>
    /// <returns>Le couple (access token, refresh token) de la session nouvellement créée.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        // L'appel au Use Case déclenche en interne la validation FluentValidation
        // et lève une DomainException si l'email existe déjà.
        AuthTokensDto tokens = await registerUserUseCase.ExecuteAsync(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, tokens);
    }

    /// <summary>
    /// Authentifie un utilisateur et génère un nouveau couple de tokens.
    /// </summary>
    /// <param name="command">Les identifiants de l'utilisateur.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Le couple (access token, refresh token).</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] AuthenticateUserCommand command,
        CancellationToken cancellationToken)
    {
        AuthTokensDto tokens = await authenticateUserUseCase.ExecuteAsync(command, cancellationToken);
        return Ok(tokens);
    }

    /// <summary>
    /// Échange un refresh token valide contre un nouveau couple de tokens (rotation : l'ancien
    /// refresh token est révoqué).
    /// </summary>
    /// <param name="command">Le refresh token courant.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Le nouveau couple (access token, refresh token).</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshAccessTokenCommand command,
        CancellationToken cancellationToken)
    {
        AuthTokensDto tokens = await refreshAccessTokenUseCase.ExecuteAsync(command, cancellationToken);
        return Ok(tokens);
    }

    /// <summary>
    /// Révoque le refresh token courant de l'utilisateur authentifié.
    /// </summary>
    /// <param name="command">Le refresh token à révoquer.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>204 No Content, systématiquement.</returns>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshAccessTokenCommand command,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        await logoutUserUseCase.ExecuteAsync(new LogoutUserCommand(userId, command.RefreshToken), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Profil de l'utilisateur authentifié (restauration de session au chargement du front).
    /// </summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Le profil de l'utilisateur courant.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        UserProfileDto profile = await getCurrentUserUseCase.ExecuteAsync(userId, cancellationToken);
        return Ok(profile);
    }
}
