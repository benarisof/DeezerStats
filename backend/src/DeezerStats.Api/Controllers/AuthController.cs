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
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        AuthTokensDto tokens = await registerUserUseCase.ExecuteAsync(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, tokens);
    }

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

    // Rotation : l'ancien refresh token est révoqué.
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

    // Restauration de session au chargement du front.
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
