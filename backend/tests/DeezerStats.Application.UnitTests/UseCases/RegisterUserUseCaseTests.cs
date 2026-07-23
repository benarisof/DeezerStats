using DeezerStats.Application.Common;
using DeezerStats.Application.Common.Exceptions;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Application.UseCases.Users;
using DeezerStats.Domain.Aggregates.UserAggregate;
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
        private readonly Mock<IAuthTokenIssuer> _authTokenIssuerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IValidator<RegisterUserCommand>> _validatorMock;

        private readonly RegisterUserUseCase _useCase;

        public RegisterUserUseCaseTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _authTokenIssuerMock = new Mock<IAuthTokenIssuer>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _validatorMock = new Mock<IValidator<RegisterUserCommand>>();

            _validatorMock
                .Setup(x => x.ValidateAsync(
                    It.IsAny<ValidationContext<RegisterUserCommand>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _useCase = new RegisterUserUseCase(
                _userRepositoryMock.Object,
                _passwordHasherMock.Object,
                _authTokenIssuerMock.Object,
                _unitOfWorkMock.Object,
                _validatorMock.Object);
        }

        [Fact]
        public async Task ExecuteAsyncShouldCreateUserAndIssueTokens()
        {
            // Arrange
            var command = new RegisterUserCommand(
                "user@test.com",
                "password",
                "Sofiane");

            var expectedTokens = new AuthTokensDto("jwt-token", "refresh-token", 3600);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(
                    It.IsAny<Email>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            _passwordHasherMock
                .Setup(x => x.Hash(command.Password))
                .Returns("hashed-password");

            _authTokenIssuerMock
                .Setup(x => x.IssueAsync(
                    It.Is<User>(u => u.Email.Value == command.Email),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTokens);

            // Act
            AuthTokensDto result = await _useCase.ExecuteAsync(command);

            // Assert
            Assert.Equal(expectedTokens, result);

            _userRepositoryMock.Verify(
                x => x.AddAsync(
                    It.IsAny<User>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // La création de l'utilisateur et l'émission du refresh token (AddAsync/IssueAsync ne
            // committent plus individuellement) sont persistées ensemble par ce seul appel.
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
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
            Task<AuthTokensDto> Action() => _useCase.ExecuteAsync(command);

            // Assert : ConflictException (409), pas DomainException (400) — voir ConflictException.cs.
            ConflictException exception = await Assert.ThrowsAsync<ConflictException>((Func<Task<AuthTokensDto>>)Action);

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

            _authTokenIssuerMock.Verify(
                x => x.IssueAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsyncWhenSaveChangesFailsDueToConcurrentRegistrationShouldThrowConflictException()
        {
            // Arrange : deux inscriptions concurrentes avec le même email -- la vérification préalable
            // (GetByEmailAsync) passe pour les deux, mais la contrainte d'unicité en base ne laisse
            // qu'une seule écriture réussir (voir RegisterUserUseCase, filet de sécurité contre la
            // race). AddAsync/IssueAsync ne committent plus individuellement (voir IUserRepository/
            // AuthTokenIssuer) : c'est ce SEUL SaveChangesAsync qui peut désormais échouer.
            var command = new RegisterUserCommand("user@test.com", "password", "Sofiane");

            var winningUser = new User(
                Guid.NewGuid(),
                new Email(command.Email),
                "hash-from-the-other-request",
                "Other Sofiane");

            _userRepositoryMock
                .SetupSequence(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null) // vérification initiale : personne avec cet email pour l'instant
                .ReturnsAsync(winningUser); // re-vérification après l'échec : un autre utilisateur a gagné la course

            _passwordHasherMock.Setup(x => x.Hash(command.Password)).Returns("hashed-password");
            _authTokenIssuerMock
                .Setup(x => x.IssueAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthTokensDto("jwt-token", "refresh-token", 3600));

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Contrainte d'unicité violée."));

            // Act
            Task<AuthTokensDto> Action() => _useCase.ExecuteAsync(command);

            // Assert
            ConflictException exception = await Assert.ThrowsAsync<ConflictException>((Func<Task<AuthTokensDto>>)Action);
            Assert.Equal("Un utilisateur existe déjà avec cette adresse email.", exception.Message);
        }

        [Fact]
        public async Task ExecuteAsyncWhenSaveChangesFailsForAnUnrelatedReasonShouldNotMaskItAsConflict()
        {
            // Arrange : SaveChangesAsync échoue, mais pour une raison SANS rapport avec l'email (ex.
            // panne de connexion) -- ne doit jamais être masqué sous un faux 409 (voir
            // RegisterUserUseCase, qui ne retraduit en ConflictException que si un AUTRE utilisateur
            // porte désormais cet email).
            var command = new RegisterUserCommand("user@test.com", "password", "Sofiane");

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            _passwordHasherMock.Setup(x => x.Hash(command.Password)).Returns("hashed-password");
            _authTokenIssuerMock
                .Setup(x => x.IssueAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthTokensDto("jwt-token", "refresh-token", 3600));

            var originalException = new InvalidOperationException("Panne de connexion à la base.");
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(originalException);

            // Act
            Task<AuthTokensDto> Action() => _useCase.ExecuteAsync(command);

            // Assert : l'exception d'origine remonte telle quelle, pas de ConflictException.
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(Action);
            Assert.Same(originalException, exception);
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

            _authTokenIssuerMock
                .Setup(x => x.IssueAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthTokensDto("jwt-token", "refresh-token", 3600));

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

            _authTokenIssuerMock
                .Setup(x => x.IssueAsync(It.IsAny<User>(), cancellationToken))
                .ReturnsAsync(new AuthTokensDto("jwt-token", "refresh-token", 3600));

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

            _authTokenIssuerMock.Verify(
                x => x.IssueAsync(It.IsAny<User>(), cancellationToken),
                Times.Once);
        }
    }
}
