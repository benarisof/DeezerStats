namespace DeezerStats.Application.Ports.BackgroundJobs
{
    /// <summary>
    /// Côté "consommateur" de l'enrichissement en tâche de fond, utilisé uniquement par le
    /// traitement en arrière-plan (voir Infrastructure.BackgroundJobs.EnrichmentBackgroundService).
    /// Séparée de <see cref="IEnrichmentJobScheduler"/> (côté "producteur", utilisé par les use
    /// cases) par souci de ségrégation des interfaces : un use case d'import n'a aucune raison de
    /// pouvoir lire les éléments en attente.
    /// </summary>
    public interface IEnrichmentJobReader
    {
        /// <summary>
        /// Consomme en continu les éléments planifiés, au fur et à mesure de leur disponibilité.
        /// </summary>
        /// <param name="ct">Jeton d'annulation (déclenché à l'arrêt de l'hôte).</param>
        /// <returns>Le flux asynchrone des éléments à enrichir, au fur et à mesure de leur disponibilité.</returns>
        public IAsyncEnumerable<EnrichmentWorkItem> ReadAllAsync(CancellationToken ct = default);
    }
}
