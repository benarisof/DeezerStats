namespace DeezerStats.Application.Ports.Catalog
{
    /// <summary>
    /// Enrichit à la demande plusieurs éléments du catalogue en parallèle (concurrence bornée, un
    /// scope DI isolé par élément pour respecter le fait qu'un DbContext EF Core n'est pas
    /// thread-safe), utilisé par les use cases de listes (GetTopAlbumsUseCase, GetTopArtistsUseCase,
    /// GetTopTracksUseCase, GetHomeStatsUseCase) qui ne peuvent pas se permettre d'enrichir leurs
    /// éléments un par un de façon séquentielle sans recréer le blocage résolu côté import (voir
    /// ImportListeningHistoryUseCase).
    /// </summary>
    public interface ICatalogEnrichmentCoordinator
    {
        /// <summary>
        /// Enrichit les albums identifiés, ré-indexe ceux dont la couverture a été mise à jour, et
        /// retourne la couverture fraîche de chacun (seuls les albums effectivement enrichis
        /// apparaissent dans le résultat).
        /// </summary>
        /// <param name="albumIds">Identifiants des albums à enrichir.</param>
        /// <param name="ct">Jeton d'annulation de l'opération.</param>
        /// <returns>Dictionnaire associant chaque identifiant d'album enrichi à sa nouvelle URL de couverture (ou <see langword="null" /> si absente).</returns>
        public Task<IReadOnlyDictionary<Guid, string?>> EnrichAlbumsAsync(IReadOnlyCollection<Guid> albumIds, CancellationToken ct = default);

        /// <summary>
        /// Équivalent de <see cref="EnrichAlbumsAsync"/> pour les artistes.
        /// </summary>
        /// <param name="artistIds">Identifiants des artistes à enrichir.</param>
        /// <param name="ct">Jeton d'annulation de l'opération.</param>
        /// <returns>Dictionnaire associant chaque identifiant d'artiste enrichi à sa nouvelle URL de couverture (ou <see langword="null" /> si absente).</returns>
        public Task<IReadOnlyDictionary<Guid, string?>> EnrichArtistsAsync(IReadOnlyCollection<Guid> artistIds, CancellationToken ct = default);

        /// <summary>
        /// Équivalent de <see cref="EnrichAlbumsAsync"/> pour les morceaux.
        /// </summary>
        /// <param name="trackIds">Identifiants des morceaux à enrichir.</param>
        /// <param name="ct">Jeton d'annulation de l'opération.</param>
        /// <returns>Dictionnaire associant chaque identifiant de morceau enrichi à sa nouvelle URL de couverture (ou <see langword="null" /> si absente).</returns>
        public Task<IReadOnlyDictionary<Guid, string?>> EnrichTracksAsync(IReadOnlyCollection<Guid> trackIds, CancellationToken ct = default);
    }
}
