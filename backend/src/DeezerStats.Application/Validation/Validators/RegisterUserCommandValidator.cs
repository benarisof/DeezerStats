using DeezerStats.Application.UseCases.Users;
using FluentValidation;

namespace DeezerStats.Application.Validation.Validators
{
    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("L'adresse email est obligatoire.")
                .EmailAddress().WithMessage("L'adresse email n'est pas valide.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Le mot de passe est obligatoire.")
                .MinimumLength(8).WithMessage("Le mot de passe doit contenir au moins 8 caractères.");

            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Le nom d'affichage est obligatoire.")
                .MaximumLength(100).WithMessage("Le nom d'affichage ne peut pas dépasser 100 caractères.");
        }
    }
}
