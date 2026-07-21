using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Application.UseCases.Users;
using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.SeedWork;
using DeezerStats.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace DeezerStats.Application.UnitTests.UseCases
{
    public class RegisterUserUseCaseTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IValidator<RegisterUserCommand>> _validatorMock;

        private readonly RegisterUserUseCase _useCase;

        public RegisterUserUseCaseTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _validatorMock = new Mock<IValidator<RegisterUserCommand>>();

            _validatorMock
                .Setup(x => x.ValidateAsync(
                    It.IsAny<ValidationContext<RegisterUserCommand>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _useCase = new RegisterUserUseCase(
                _userRepositoryMock.Object,
                _passwordHasherMock.Object,
                _validatorMock.Object);
        }

        [Fact]
        public async Task ExecuteAsyncShouldCreateUser()
        {
            // Arrange
            var command = new RegisterUserCommand(
                "user@test.com",
                "password",
                "Sofiane");

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(
                    It.IsAny<Email>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            _passwordHasherMock
                .Setup(x => x.Hash(command.Password))
                .Returns("hashed-password");

            // Act
            User user = await _useCase.ExecuteAsync(command);

            // Assert
            Assert.NotNull(user);
            Assert.Equal("user@test.com", user.Email.Value);
            Assert.Equal("Sofiane", user.DisplayName);
            Assert.Equal("hashed-password", user.PasswordHash);

            _userRepositoryMock.Verify(
                x => x.AddAsync(
                    It.IsAny<User>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsyncShouldRejectExistingEmail()
        {
            // Arrange
            var command = new RegisterUserCommand(
                "user@test.com",
                "password",
                "Sofiane");

            var existingUser = new User(
                Guid.NewGuid(),
                new Email(command.Email),
                "existing-hash",
                "Existing User");

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(
                    It.IsAny<Email>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);

            // Act
            Task<User> Action() => _useCase.ExecuteAsync(command);

            // Assert
            DomainException exception = await Assert.ThrowsAsync<DomainException>((Func<Task<User>>)Action);

            Assert.Equal(
                "Un utilisateur existe déjà avec cette adresse email.",
                exception.Message);

            _userRepositoryMock.Verify(
                x => x.AddAsync(
                    It.IsAny<User>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _passwordHasherMock.Verify(
                x => x.Hash(It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsyncShouldHashPasswordBeforeCreatingUser()
        {
            // Arrange
            var command = new RegisterUserCommand(
                "user@test.com",
                "password",
                "Sofiane");

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(
                    It.IsAny<Email>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            _passwordHasherMock
                .Setup(x => x.Hash(command.Password))
                .Returns("hashed-password");

            // Act
            await _useCase.ExecuteAsync(command);

            // Assert
            _passwordHasherMock.Verify(
                x => x.Hash(command.Password),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsyncShouldPropagateCancellationToken()
        {
            // Arrange
            var command = new RegisterUserCommand(
                "user@test.com",
                "password",
                "Sofiane");

            using var cancellationTokenSource = new CancellationTokenSource();

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(
                    It.IsAny<Email>(),
                    cancellationToken))
                .ReturnsAsync((User?)null);

            _passwordHasherMock
                .Setup(x => x.Hash(command.Password))
                .Returns("hashed-password");

            // Act
            await _useCase.ExecuteAsync(
                command,
                cancellationToken);

            // Assert
            _userRepositoryMock.Verify(
                x => x.GetByEmailAsync(
                    It.IsAny<Email>(),
                    cancellationToken),
                Times.Once);

            _userRepositoryMock.Verify(
                x => x.AddAsync(
                    It.IsAny<User>(),
                    cancellationToken),
                Times.Once);
        }
    }
}
