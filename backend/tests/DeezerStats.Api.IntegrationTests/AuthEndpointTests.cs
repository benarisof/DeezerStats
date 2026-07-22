using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.UseCases.Users;
using FluentAssertions;

namespace DeezerStats.Api.IntegrationTests;

/// <summary>
/// Tests d'intégration bout-en-bout du parcours d'authentification complet (Phase 6, ticket 6.5) :
/// register -> login -> refresh -> logout -> me, en hébergeant la vraie API (voir
/// CustomWebApplicationFactory), sans mocker les use cases, la validation, ni le middleware JWT.
/// </summary>
public class AuthEndpointTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory = factory;

    [Fact]
    public async Task FullAuthJourneyShouldSucceedEndToEnd()
    {
        // Arrange
        using HttpClient client = _factory.CreateClient();
        var registerCommand = new RegisterUserCommand("journey@example.com", "StrongPass123!", "Journey User");

        // Act 1 : register -> connexion automatique (201 + tokens).
        using HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerCommand);

        // Assert
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        AuthTokensDto? registerTokens = await registerResponse.Content.ReadFromJsonAsync<AuthTokensDto>();
        registerTokens.Should().NotBeNull();
        registerTokens!.AccessToken.Should().NotBeNullOrEmpty();
        registerTokens.RefreshToken.Should().NotBeNullOrEmpty();

        // Act 2 : login avec les mêmes identifiants -> nouveau couple de tokens.
        var loginCommand = new AuthenticateUserCommand("journey@example.com", "StrongPass123!");
        using HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginCommand);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokensDto? loginTokens = await loginResponse.Content.ReadFromJsonAsync<AuthTokensDto>();
        loginTokens.Should().NotBeNull();

        // Act 3 : GET /auth/me avec l'access token du login.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginTokens!.AccessToken);
        using HttpResponseMessage meResponse = await client.GetAsync("/api/v1/auth/me");

        // Assert
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        UserProfileDto? profile = await meResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be("journey@example.com");
        profile.DisplayName.Should().Be("Journey User");

        // Act 4 : refresh avec le refresh token du login -> nouveau couple de tokens (rotation).
        var refreshCommand = new RefreshAccessTokenCommand(loginTokens.RefreshToken);
        using HttpResponseMessage refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", refreshCommand);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokensDto? refreshedTokens = await refreshResponse.Content.ReadFromJsonAsync<AuthTokensDto>();
        refreshedTokens.Should().NotBeNull();
        refreshedTokens!.RefreshToken.Should().NotBe(loginTokens.RefreshToken, "la rotation doit émettre un nouveau refresh token");

        // Act 5 : le refresh token du login (désormais révoqué par la rotation) ne doit plus fonctionner.
        using HttpResponseMessage reuseResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", refreshCommand);

        // Assert
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Act 6 : logout avec le refresh token courant (issu de la rotation), authentifié avec le
        // nouvel access token.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshedTokens.AccessToken);
        var logoutCommand = new RefreshAccessTokenCommand(refreshedTokens.RefreshToken);
        using HttpResponseMessage logoutResponse = await client.PostAsJsonAsync("/api/v1/auth/logout", logoutCommand);

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act 7 : le refresh token révoqué par le logout ne doit plus fonctionner.
        using HttpResponseMessage refreshAfterLogoutResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", logoutCommand);

        // Assert
        refreshAfterLogoutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterWithAlreadyUsedEmailShouldReturn409Conflict()
    {
        // Arrange
        using HttpClient client = _factory.CreateClient();
        var command = new RegisterUserCommand("duplicate@example.com", "StrongPass123!", "First User");
        using HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/v1/auth/register", command);
        firstResponse.EnsureSuccessStatusCode();

        // Act
        using HttpResponseMessage secondResponse = await client.PostAsJsonAsync("/api/v1/auth/register", command);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task LoginWithWrongPasswordShouldReturn401Unauthorized()
    {
        // Arrange
        using HttpClient client = _factory.CreateClient();
        var registerCommand = new RegisterUserCommand("wrongpass@example.com", "StrongPass123!", "Test User");
        using HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerCommand);
        registerResponse.EnsureSuccessStatusCode();

        // Act
        var loginCommand = new AuthenticateUserCommand("wrongpass@example.com", "WrongPassword!");
        using HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginCommand);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MeWithoutTokenShouldReturn401Unauthorized()
    {
        // Arrange
        using HttpClient client = _factory.CreateClient();

        // Act
        using HttpResponseMessage response = await client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshWithInvalidTokenShouldReturn401Unauthorized()
    {
        // Arrange
        using HttpClient client = _factory.CreateClient();

        // Act
        var refreshCommand = new RefreshAccessTokenCommand("this-token-does-not-exist");
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/refresh", refreshCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
