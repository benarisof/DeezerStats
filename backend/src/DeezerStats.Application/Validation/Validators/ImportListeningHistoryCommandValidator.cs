using DeezerStats.Application.UseCases.Imports;
using FluentValidation;

namespace DeezerStats.Application.Validation.Validators
{
    public class ImportListeningHistoryCommandValidator : AbstractValidator<ImportListeningHistoryCommand>
    {
        public ImportListeningHistoryCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("L'identifiant de l'utilisateur est obligatoire.");

            RuleFor(x => x.FileStream)
                .NotNull()
                .WithMessage("Le fichier d'importation ne peut pas être nul.")
                .Must(stream => stream != null && stream.Length > 0)
                .WithMessage("Le fichier transmis est vide.");
        }
    }
}
