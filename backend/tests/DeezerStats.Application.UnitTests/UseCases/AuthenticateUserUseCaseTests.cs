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
    public class AuthenticateUserUseCaseTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IAuthTokenIssuer> _authTokenIssuerMock;

        private readonly AuthenticateUserUseCase _useCase;

        public AuthenticateUserUseCaseTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _authTokenIssuerMock = new Mock<IAuthTokenIssuer>();

            _useCase = new AuthenticateUserUseCase(
                _userRepositoryMock.Object,
                _passwordHasherMock.Object,
                _authTokenIssuerMock.Object,
                new AuthenticateUserCommandValidator());
        }

        [Fact]
        public async Task ExecuteAsyncShouldAuthenticateUserWithValidCredentials()
        {
            // Arrange
            var command = new AuthenticateUserCommand(
                "user@test.com",
                "password123");

            var user = new User(
                Guid.NewGuid(),
                new Email(command.Email),
                "hashed-password",
                "Sofiane");

            var expectedTokens = new AuthTokensDto("jwt-token", "refresh-token", 3600);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(
                    It.IsAny<Email>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(
                    command.Password,
                    user.PasswordHash))
                .Returns(true);

            _authTokenIssuerMock
                .Setup(x => x.IssueAsync(user, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTokens);

            // Act
            AuthTokensDto result = await _useCase.ExecuteAsync(command);

            // Assert
            Assert.Equal(expectedTokens, result);

            _passwordHasherMock.Verify(
                x => x.Verify(
                    command.Password,
                    user.PasswordHash),
                Times.Once);

            _authTokenIssuerMock.Verify(
                x => x.IssueAsync(user, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsyncShouldRejectUnknownEmail()
        {
            // Arrange
            var command = new AuthenticateUserCommand(
                "unknown@test.com",
                "password123");

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(
                    It.IsAny<Email>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            Task Action() => _useCase.ExecuteAsync(command);

            // Assert
            await Assert.ThrowsAsync<AuthenticationFailedException>(
                Action);

            _passwordHasherMock.Verify(
                x => x.Verify(
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);

            _authTokenIssuerMock.Verify(
                x => x.IssueAsync(
                    It.IsAny<User>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsyncShouldRejectInvalidPassword()
        {
            // Arrange
            var command = new AuthenticateUserCommand(
                "user@test.com",
                "wrong-password");

            var user = new User(
                Guid.NewGuid(),
                new Email(command.Email),
                "hashed-password",
                "Sofiane");

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(
                    It.IsAny<Email>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(
                    command.Password,
                    user.PasswordHash))
                .Returns(false);

            // Act
            Task Action() => _useCase.ExecuteAsync(command);

            // Assert
            await Assert.ThrowsAsync<AuthenticationFailedException>(
                Action);

            _authTokenIssuerMock.Verify(
                x => x.IssueAsync(
                    It.IsAny<User>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsyncShouldRejectInvalidCommand()
        {
            // Arrange
            var command = new AuthenticateUserCommand(
                "invalid-email",
                string.Empty);

            // Act
            Task Action() => _useCase.ExecuteAsync(command);

            // Assert
            await Assert.ThrowsAsync<FluentValidation.ValidationException>(
                Action);

            _userRepositoryMock.Verify(
                x => x.GetByEmailAsync(
                    It.IsAny<Email>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _authTokenIssuerMock.Verify(
                x => x.IssueAsync(
                    It.IsAny<User>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
