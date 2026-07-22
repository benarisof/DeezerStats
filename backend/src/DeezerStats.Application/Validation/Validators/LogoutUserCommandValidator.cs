using DeezerStats.Application.UseCases.Users;
using FluentValidation;

namespace DeezerStats.Application.Validation.Validators
{
    public class LogoutUserCommandValidator : AbstractValidator<LogoutUserCommand>
    {
        public LogoutUserCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Le refresh token est obligatoire.");
        }
    }
}
