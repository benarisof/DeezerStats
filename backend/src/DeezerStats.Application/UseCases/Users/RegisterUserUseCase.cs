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

            // AddAsync/IssueAsync ne font que suivre les nouvelles entités : création de l'utilisateur
            // et émission du refresh token sont persistées ensemble par le seul SaveChangesAsync
            // ci-dessous, atomiquement.
            await _userRepository.AddAsync(user, ct);

            // Connexion automatique après inscription.
            AuthTokensDto tokens = await _authTokenIssuer.IssueAsync(user, ct);

            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Filet de sécurité contre une course : la vérification "existingUser is not null"
                // ci-dessus n'est pas atomique avec cette écriture. On ne retraduit en ConflictException
                // QUE si un autre utilisateur porte désormais cet email, pour ne pas masquer une panne
                // de base sans rapport sous un 409.
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
