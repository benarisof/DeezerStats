using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DeezerStats.Api.Controllers;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.UseCases.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DeezerStats.Api.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly IRegisterUserUseCase _registerUseCaseMock;
    private readonly IAuthenticateUserUseCase _authenticateUseCaseMock;
    private readonly IRefreshAccessTokenUseCase _refreshUseCaseMock;
    private readonly ILogoutUserUseCase _logoutUseCaseMock;
    private readonly IGetCurrentUserUseCase _getCurrentUserUseCaseMock;
    private readonly AuthController _authController;

    public AuthControllerTests()
    {
        // Création des bouchons (Mocks) avec NSubstitute
        _registerUseCaseMock = Substitute.For<IRegisterUserUseCase>();
        _authenticateUseCaseMock = Substitute.For<IAuthenticateUserUseCase>();
        _refreshUseCaseMock = Substitute.For<IRefreshAccessTokenUseCase>();
        _logoutUseCaseMock = Substitute.For<ILogoutUserUseCase>();
        _getCurrentUserUseCaseMock = Substitute.For<IGetCurrentUserUseCase>();

        // Injection des bouchons dans le contrôleur
        _authController = new AuthController(
            _registerUseCaseMock,
            _authenticateUseCaseMock,
            _refreshUseCaseMock,
            _logoutUseCaseMock,
            _getCurrentUserUseCaseMock);
    }

    [Fact]
    public async Task RegisterShouldReturn201CreatedWithTokensWhenCommandIsValid()
    {
        // Arrange
        var command = new RegisterUserCommand("test@deezer.com", "StrongPass123!", "Test User");
        var expectedTokens = new AuthTokensDto("access-token", "refresh-token", 3600);

        _registerUseCaseMock
            .ExecuteAsync(command, Arg.Any<CancellationToken>())
            .Returns(expectedTokens);

        // Act
        IActionResult result = await _authController.Register(command, CancellationToken.None);

        // Assert
        ObjectResult objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        objectResult.Value.Should().BeEquivalentTo(expectedTokens);

        await _registerUseCaseMock.Received(1).ExecuteAsync(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginShouldReturn200OkWithTokensWhenCredentialsAreValid()
    {
        // Arrange
        var command = new AuthenticateUserCommand("test@deezer.com", "StrongPass123!");
        var expectedTokens = new AuthTokensDto("access-token", "refresh-token", 3600);

        _authenticateUseCaseMock
            .ExecuteAsync(command, Arg.Any<CancellationToken>())
            .Returns(expectedTokens);

        // Act
        IActionResult result = await _authController.Login(command, CancellationToken.None);

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        okResult.Value.Should().BeEquivalentTo(expectedTokens);

        await _authenticateUseCaseMock.Received(1).ExecuteAsync(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshShouldReturn200OkWithNewTokensWhenRefreshTokenIsValid()
    {
        // Arrange
        var command = new RefreshAccessTokenCommand("valid-refresh-token");
        var expectedTokens = new AuthTokensDto("new-access-token", "new-refresh-token", 3600);

        _refreshUseCaseMock
            .ExecuteAsync(command, Arg.Any<CancellationToken>())
            .Returns(expectedTokens);

        // Act
        IActionResult result = await _authController.Refresh(command, CancellationToken.None);

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedTokens);

        await _refreshUseCaseMock.Received(1).ExecuteAsync(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogoutShouldReturn204NoContentAndRevokeTokenForAuthenticatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RefreshAccessTokenCommand("current-refresh-token");
        SetAuthenticatedUser(userId);

        // Act
        IActionResult result = await _authController.Logout(command, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        await _logoutUseCaseMock.Received(1).ExecuteAsync(
            Arg.Is<LogoutUserCommand>(c => c != null && c.UserId == userId && c.RefreshToken == command.RefreshToken),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MeShouldReturn200OkWithProfileOfAuthenticatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedProfile = new UserProfileDto(userId, "test@deezer.com", "Test User");
        SetAuthenticatedUser(userId);

        _getCurrentUserUseCaseMock
            .ExecuteAsync(userId, Arg.Any<CancellationToken>())
            .Returns(expectedProfile);

        // Act
        IActionResult result = await _authController.Me(CancellationToken.None);

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedProfile);
    }

    private void SetAuthenticatedUser(Guid userId)
    {
        var claimsIdentity = new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            authenticationType: "TestAuth");

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(claimsIdentity),
            },
        };
    }
}
