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

            // AddAsync/IssueAsync ne font que suivre les nouvelles entités (aucun accès base, voir
            // IUserRepository.AddAsync et AuthTokenIssuer) : la création de l'utilisateur et
            // l'émission du refresh token sont donc persistées ensemble par le SEUL SaveChangesAsync
            // ci-dessous, atomiquement -- plus besoin de transaction explicite pour éviter qu'un
            // utilisateur créé sans session ne puisse plus jamais se réinscrire (email déjà pris) ni
            // obtenir de tokens.
            await _userRepository.AddAsync(user, ct);

            // Connexion automatique après inscription (voir schéma AuthTokens en réponse de POST
            // /auth/register dans le contrat OpenAPI).
            AuthTokensDto tokens = await _authTokenIssuer.IssueAsync(user, ct);

            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Filet de sécurité contre une course : la vérification "existingUser is not null"
                // ci-dessus n'est pas atomique avec cette écriture, deux inscriptions concurrentes
                // avec le même email peuvent donc toutes les deux la passer. Seule la contrainte
                // d'unicité en base (voir UserConfiguration.HasAlternateKey) peut trancher avec
                // certitude ; on ne retraduit en ConflictException QUE si un autre utilisateur porte
                // désormais cet email, pour ne pas masquer une panne de base sans rapport sous un 409.
                User? conflictingUser = await _userRepository.GetByEmailAsync(email, ct);

                if (conflictingUser is not null && conflictingUser.Id != user.Id)
                {
                    throw new ConflictException("Un utilisateur existe déjà avec cette adresse email.", ex);
                }

                throw;
            }

            return tokens;
        }
    }
}
