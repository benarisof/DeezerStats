using DeezerStats.Application.Common;
using DeezerStats.Application.Common.Exceptions;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.ValueObjects;
using FluentValidation;

namespace DeezerStats.Application.UseCases.Users
{
    public class RegisterUserUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthTokenIssuer authTokenIssuer,
        IUnitOfWork unitOfWork,
        IValidator<RegisterUserCommand> validator) : IRegisterUserUseCase
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly IAuthTokenIssuer _authTokenIssuer = authTokenIssuer;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IValidator<RegisterUserCommand> _validator = validator;

        public async Task<AuthTokensDto> ExecuteAsync(
            RegisterUserCommand command,
            CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(command, ct);

            var email = new Email(command.Email);

            User? existingUser = await _userRepository
                .GetByEmailAsync(email, ct);

            if (existingUser is not null)
            {
                throw new ConflictException(
                    "Un utilisateur existe déjà avec cette adresse email.");
            }

            var passwordHash = _passwordHasher.Hash(command.Password);

            var user = new User(
                Guid.NewGuid(),
                email,
                passwordHash,
                command.DisplayName);

            // La création de l'utilisateur et l'émission du refresh token sont deux écritures
            // indépendantes (chacune via son propre repository/SaveChangesAsync) : sans transaction
            // explicite, un échec du second appel laisserait un utilisateur créé sans session, ne
            // pouvant plus jamais se réinscrire (email déjà pris) ni obtenir de tokens. Voir
            // IUnitOfWork.ExecuteInTransactionAsync.
            AuthTokensDto? tokens = null;
            await _unitOfWork.ExecuteInTransactionAsync(
                async () =>
                {
                    await _userRepository.AddAsync(user, ct);

                    // Connexion automatique après inscription (voir schéma AuthTokens en réponse de
                    // POST /auth/register dans le contrat OpenAPI).
                    tokens = await _authTokenIssuer.IssueAsync(user, ct);
                },
                ct);

            return tokens!;
        }
    }
}
