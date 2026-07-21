namespace DeezerStats.Application.UseCases.Stats
{
    /// <summary>
    /// Contrat commun aux requêtes paginées de consultation (tops, historique), pour factoriser
    /// leur validation (voir Validation.Validators.PagedQueryValidator{T}).
    /// </summary>
    public interface IPagedQuery
    {
        public int Page { get; }

        public int PageSize { get; }
    }
}
