namespace DeezerStats.Application.Ports.BackgroundJobs
{
    /// <summary>
    /// Côté "producteur" de l'enrichissement en tâche de fond : permet à un use case (ex.
    /// ImportListeningHistoryUseCase) de demander l'enrichissement Deezer d'un morceau ou d'un
    /// album sans attendre le résultat, conformément au contrat OpenAPI de POST /imports
    /// ("l'enrichissement Deezer [...] se fait en tâche de fond [...] et n'est donc pas garanti
    /// immédiat"). Séparée de <see cref="IEnrichmentJobReader"/> (côté "consommateur", utilisé
    /// uniquement par le traitement en arrière-plan) par souci de ségrégation des interfaces.
    /// </summary>
    public interface IEnrichmentJobScheduler
    {
        /// <summary>
        /// Planifie un élément de travail à traiter en arrière-plan.
        /// </summary>
        /// <param name="item">Élément à enrichir (morceau ou album).</param>
        /// <param name="ct">Jeton d'annulation pour l'opération asynchrone.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public ValueTask EnqueueAsync(EnrichmentWorkItem item, CancellationToken ct = default);
    }
}
