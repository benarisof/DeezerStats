using DeezerStats.Application.Common;
using DeezerStats.Application.Common.Exceptions;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Application.UseCases.Users;
using DeezerStats.Application.Validation.Validators;
using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.ValueObjects;
using Moq;

namespace DeezerStats.Application.UnitTests.UseCases
{
    public class RefreshAccessTokenUseCaseTests
    {
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
        private readonly Mock<IRefreshTokenGenerator> _refreshTokenGeneratorMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IAuthTokenIssuer> _authTokenIssuerMock;

        private readonly RefreshAccessTokenUseCase _useCase;

        public RefreshAccessTokenUseCaseTests()
        {
            _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            _refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _authTokenIssuerMock = new Mock<IAuthTokenIssuer>();

            _refreshTokenGeneratorMock
                .Setup(x => x.Hash(It.IsAny<string>()))
                .Returns<string>(raw => $"hashed-{raw}");

            _useCase = new RefreshAccessTokenUseCase(
                _refreshTokenRepositoryMock.Object,
                _refreshTokenGeneratorMock.Object,
                _userRepositoryMock.Object,
                _authTokenIssuerMock.Object,
                new RefreshAccessTokenCommandValidator());
        }

        [Fact]
        public async Task ExecuteAsyncShouldRotateTokenAndReturnNewTokensWhenRefreshTokenIsValid()
        {
            // Arrange
            var command = new RefreshAccessTokenCommand("raw-refresh-token");
            var userId = Guid.NewGuid();

            var existingToken = new RefreshToken(
                Guid.NewGuid(),
                userId,
                "hashed-raw-refresh-token",
                DateTime.UtcNow.AddDays(1));

            var user = new User(userId, new Email("user@test.com"), "hash", "Sofiane");
            var expectedTokens = new AuthTokensDto("new-access", "new-refresh", 3600);

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync("hashed-raw-refresh-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingToken);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _authTokenIssuerMock
                .Setup(x => x.IssueAsync(user, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTokens);

            // Act
            AuthTokensDto result = await _useCase.ExecuteAsync(command);

            // Assert
            Assert.Equal(expectedTokens, result);
            Assert.True(existingToken.IsRevoked, "l'ancien refresh token doit être révoqué (rotation).");

            _refreshTokenRepositoryMock.Verify(
                x => x.UpdateAsync(existingToken, It.IsAny<CancellationToken>()),
                Times.Once);

            _refreshTokenRepositoryMock.Verify(
                x => x.RevokeAllActiveForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsyncShouldRejectUnknownToken()
        {
            // Arrange
            var command = new RefreshAccessTokenCommand("unknown-token");

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);

            // Act
            Task Action() => _useCase.ExecuteAsync(command);

            // Assert
            await Assert.ThrowsAsync<AuthenticationFailedException>(Action);
        }

        [Fact]
        public async Task ExecuteAsyncShouldRejectExpiredToken()
        {
            // Arrange
            var command = new RefreshAccessTokenCommand("expired-token");

            var expiredToken = new RefreshToken(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "hashed-expired-token",
                DateTime.UtcNow.AddDays(-1));

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync("hashed-expired-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(expiredToken);

            // Act
            Task Action() => _useCase.ExecuteAsync(command);

            // Assert
            await Assert.ThrowsAsync<AuthenticationFailedException>(Action);

            _authTokenIssuerMock.Verify(
                x => x.IssueAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsyncShouldRevokeAllActiveTokensWhenAlreadyRevokedTokenIsReused()
        {
            // Arrange : réutilisation d'un token déjà révoqué (rotation précédente) -> vol probable.
            var command = new RefreshAccessTokenCommand("reused-token");
            var userId = Guid.NewGuid();

            var revokedToken = new RefreshToken(
                Guid.NewGuid(),
                userId,
                "hashed-reused-token",
                DateTime.UtcNow.AddDays(1));
            revokedToken.Revoke();

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync("hashed-reused-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(revokedToken);

            // Act
            Task Action() => _useCase.ExecuteAsync(command);

            // Assert
            await Assert.ThrowsAsync<AuthenticationFailedException>(Action);

            _refreshTokenRepositoryMock.Verify(
                x => x.RevokeAllActiveForUserAsync(userId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsyncShouldRejectInvalidCommand()
        {
            // Arrange
            var command = new RefreshAccessTokenCommand(string.Empty);

            // Act
            Task Action() => _useCase.ExecuteAsync(command);

            // Assert
            await Assert.ThrowsAsync<FluentValidation.ValidationException>(Action);

            _refreshTokenRepositoryMock.Verify(
                x => x.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
