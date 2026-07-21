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
    private readonly AuthController _authController;

    public AuthControllerTests()
    {
        // Création des bouchons (Mocks) avec NSubstitute
        _registerUseCaseMock = Substitute.For<IRegisterUserUseCase>();
        _authenticateUseCaseMock = Substitute.For<IAuthenticateUserUseCase>();

        // Injection des bouchons dans le contrôleur
        _authController = new AuthController(_registerUseCaseMock, _authenticateUseCaseMock);
    }

    [Fact]
    public async Task RegisterShouldReturn201CreatedWhenCommandIsValid()
    {
        // Arrange
        var command = new RegisterUserCommand("test@deezer.com", "StrongPass123!", "Test User");

        // Act
        IActionResult result = await _authController.Register(command, CancellationToken.None);

        // Assert
        StatusCodeResult statusCodeResult = result.Should().BeOfType<StatusCodeResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status201Created);

        // Vérifie que le UseCase a bien été appelé une fois avec les bons paramètres
        await _registerUseCaseMock.Received(1).ExecuteAsync(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginShouldReturn200OkWithTokenWhenCredentialsAreValid()
    {
        // Arrange
        var command = new AuthenticateUserCommand("test@deezer.com", "StrongPass123!");
        DateTime expirationTime = DateTime.UtcNow.AddMinutes(60);
        var expectedToken = new AccessTokenDto("eyJhbGciOiJIUzI1NiIsInR...", expirationTime);

        // Configuration du mock pour qu'il retourne un faux token
        _authenticateUseCaseMock
            .ExecuteAsync(command, Arg.Any<CancellationToken>())
            .Returns(expectedToken);

        // Act
        IActionResult result = await _authController.Login(command, CancellationToken.None);

        // Assert
        OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        // Vérifie que la valeur retournée est bien le token
        okResult.Value.Should().BeEquivalentTo(expectedToken);

        await _authenticateUseCaseMock.Received(1).ExecuteAsync(command, Arg.Any<CancellationToken>());
    }
}
