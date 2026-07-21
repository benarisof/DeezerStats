using DeezerStats.Application.UseCases.Users;
using FluentValidation;

namespace DeezerStats.Application.Validation.Validators
{
    public class AuthenticateUserCommandValidator
            : AbstractValidator<AuthenticateUserCommand>
    {
        public AuthenticateUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("L'adresse email est obligatoire.")
                .EmailAddress()
                .WithMessage("L'adresse email n'est pas valide.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Le mot de passe est obligatoire.");
        }
    }
}
