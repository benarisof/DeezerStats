using DeezerStats.Application.Common.Exceptions;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.UseCases.Users;
using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.ValueObjects;
using Moq;

namespace DeezerStats.Application.UnitTests.UseCases
{
    public class GetCurrentUserUseCaseTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly GetCurrentUserUseCase _useCase;

        public GetCurrentUserUseCaseTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _useCase = new GetCurrentUserUseCase(_userRepositoryMock.Object);
        }

        [Fact]
        public async Task ExecuteAsyncShouldReturnProfileOfExistingUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User(userId, new Email("user@test.com"), "hash", "Sofiane");

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            UserProfileDto profile = await _useCase.ExecuteAsync(userId);

            // Assert
            Assert.Equal(userId, profile.Id);
            Assert.Equal("user@test.com", profile.Email);
            Assert.Equal("Sofiane", profile.DisplayName);
        }

        [Fact]
        public async Task ExecuteAsyncShouldRejectUnknownUser()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            Task Action() => _useCase.ExecuteAsync(userId);

            // Assert : 401, pas 404 -- voir GetCurrentUserUseCase.
            await Assert.ThrowsAsync<AuthenticationFailedException>(Action);
        }
    }
}
