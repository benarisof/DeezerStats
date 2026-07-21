using DeezerStats.Application.Common.Exceptions;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.ValueObjects;
using FluentValidation;

namespace DeezerStats.Application.UseCases.Users
{
    public class AuthenticateUserUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokenGenerator,
        IValidator<AuthenticateUserCommand> validator) : IAuthenticateUserUseCase
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly IAccessTokenGenerator _accessTokenGenerator =
            accessTokenGenerator;

        private readonly IValidator<AuthenticateUserCommand> _validator = validator;

        public async Task<AccessTokenDto> ExecuteAsync(
            AuthenticateUserCommand command,
            CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(command, ct);

            var email = new Email(command.Email);
            User? user = await _userRepository
                .GetByEmailAsync(email, ct) ?? throw new AuthenticationFailedException(
                    "Email ou mot de passe invalide.");
            var isPasswordValid = _passwordHasher.Verify(
                command.Password,
                user.PasswordHash);

            if (!isPasswordValid)
            {
                throw new AuthenticationFailedException(
                    "Email ou mot de passe invalide.");
            }

            return _accessTokenGenerator.Generate(user);
        }
    }
}
