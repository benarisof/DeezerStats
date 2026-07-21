using DeezerStats.Application.Common;
using DeezerStats.Application.UseCases.Stats;
using FluentValidation;

namespace DeezerStats.Application.Validation.Validators
{
    /// <summary>
    /// Règles de validation communes à toutes les requêtes paginées de consultation (tops,
    /// historique) : factorisées ici plutôt que dupliquées dans chaque validateur concret, qui n'a
    /// donc rien à ajouter (voir GetTopAlbumsQueryValidator et consorts).
    /// </summary>
    /// <typeparam name="T">Type de la requête paginée à valider.</typeparam>
    public abstract class PagedQueryValidator<T> : AbstractValidator<T>
        where T : IPagedQuery
    {
        protected PagedQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Le numéro de page doit être supérieur ou égal à 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, StatsRules.MaxRankedResults)
                .WithMessage($"La taille de page doit être comprise entre 1 et {StatsRules.MaxRankedResults}.");
        }
    }
}
