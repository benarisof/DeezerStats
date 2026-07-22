using DeezerStats.Application.UseCases.Users;
using FluentValidation;

namespace DeezerStats.Application.Validation.Validators
{
    public class RefreshAccessTokenCommandValidator : AbstractValidator<RefreshAccessTokenCommand>
    {
        public RefreshAccessTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Le refresh token est obligatoire.");
        }
    }
}
