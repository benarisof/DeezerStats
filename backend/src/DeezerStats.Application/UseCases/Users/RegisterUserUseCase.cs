using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.SeedWork;
using DeezerStats.Domain.ValueObjects;
using FluentValidation;

namespace DeezerStats.Application.UseCases.Users
{
    public class RegisterUserUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IValidator<RegisterUserCommand> validator) : IRegisterUserUseCase
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly IValidator<RegisterUserCommand> _validator = validator;

        public async Task<User> ExecuteAsync(
            RegisterUserCommand command,
            CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(command, ct);

            var email = new Email(command.Email);

            User? existingUser = await _userRepository
                .GetByEmailAsync(email, ct);

            if (existingUser is not null)
            {
                throw new DomainException(
                    "Un utilisateur existe déjà avec cette adresse email.");
            }

            var passwordHash = _passwordHasher.Hash(command.Password);

            var user = new User(
                Guid.NewGuid(),
                email,
                passwordHash,
                command.DisplayName);

            await _userRepository.AddAsync(user, ct);

            return user;
        }
    }
}
