namespace DeezerStats.Application.Ports.Catalog
{
    /// <summary>
    /// Enrichit à la demande plusieurs éléments du catalogue en parallèle (concurrence bornée, un
    /// scope DI isolé par élément car un DbContext EF Core n'est pas thread-safe), pour les use
    /// cases de listes (tops, accueil) qui ne peuvent pas se permettre d'enrichir séquentiellement.
    /// Chaque méthode retourne un dictionnaire id -> nouvelle couverture, limité aux éléments
    /// effectivement enrichis, et ré-indexe ceux dont la couverture a changé.
    /// </summary>
    public interface ICatalogEnrichmentCoordinator
    {
        public Task<IReadOnlyDictionary<Guid, string?>> EnrichAlbumsAsync(IReadOnlyCollection<Guid> albumIds, CancellationToken ct = default);

        public Task<IReadOnlyDictionary<Guid, string?>> EnrichArtistsAsync(IReadOnlyCollection<Guid> artistIds, CancellationToken ct = default);

        public Task<IReadOnlyDictionary<Guid, string?>> EnrichTracksAsync(IReadOnlyCollection<Guid> trackIds, CancellationToken ct = default);
    }
}
