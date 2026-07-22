using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Application.UseCases.Users;
using DeezerStats.Application.Validation.Validators;
using DeezerStats.Domain.Aggregates.UserAggregate;
using Moq;

namespace DeezerStats.Application.UnitTests.UseCases
{
    public class LogoutUserUseCaseTests
    {
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
        private readonly Mock<IRefreshTokenGenerator> _refreshTokenGeneratorMock;

        private readonly LogoutUserUseCase _useCase;

        public LogoutUserUseCaseTests()
        {
            _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            _refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();

            _refreshTokenGeneratorMock
                .Setup(x => x.Hash(It.IsAny<string>()))
                .Returns<string>(raw => $"hashed-{raw}");

            _useCase = new LogoutUserUseCase(
                _refreshTokenRepositoryMock.Object,
                _refreshTokenGeneratorMock.Object,
                new LogoutUserCommandValidator());
        }

        [Fact]
        public async Task ExecuteAsyncShouldRevokeTokenBelongingToUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new LogoutUserCommand(userId, "raw-refresh-token");

            var existingToken = new RefreshToken(
                Guid.NewGuid(),
                userId,
                "hashed-raw-refresh-token",
                DateTime.UtcNow.AddDays(1));

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync("hashed-raw-refresh-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingToken);

            // Act
            await _useCase.ExecuteAsync(command);

            // Assert
            Assert.True(existingToken.IsRevoked);

            _refreshTokenRepositoryMock.Verify(
                x => x.UpdateAsync(existingToken, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsyncShouldBeSilentWhenTokenDoesNotExist()
        {
            // Arrange
            var command = new LogoutUserCommand(Guid.NewGuid(), "unknown-token");

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);

            // Act & Assert : ne doit lever aucune exception (voir contrat OpenAPI, 204 systématique).
            await _useCase.ExecuteAsync(command);

            _refreshTokenRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsyncShouldNotRevokeTokenBelongingToAnotherUser()
        {
            // Arrange : le token existe mais appartient à un autre utilisateur -- ne doit jamais être
            // révoqué par la session courante (même s'il présente une valeur brute valide).
            var command = new LogoutUserCommand(Guid.NewGuid(), "raw-refresh-token");

            var tokenOfAnotherUser = new RefreshToken(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "hashed-raw-refresh-token",
                DateTime.UtcNow.AddDays(1));

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync("hashed-raw-refresh-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(tokenOfAnotherUser);

            // Act
            await _useCase.ExecuteAsync(command);

            // Assert
            Assert.False(tokenOfAnotherUser.IsRevoked);

            _refreshTokenRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
