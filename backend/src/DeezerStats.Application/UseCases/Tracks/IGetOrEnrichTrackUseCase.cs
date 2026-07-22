using DeezerStats.Domain.Aggregates.TrackAggregate;

namespace DeezerStats.Application.UseCases.Tracks
{
    public interface IGetOrEnrichTrackUseCase
    {
        public Task<Track?> ExecuteAsync(GetOrEnrichTrackRequest request, CancellationToken ct = default);

        /// <summary>
        /// Variante recherchant le morceau par son identifiant plutôt que par son ISRC, utilisée
        /// quand l'appelant connaît déjà le morceau en base (ex. CatalogEnrichmentCoordinator, qui
        /// part des résultats déjà paginés d'un top morceaux).
        /// </summary>
        /// <param name="trackId">Identifiant du morceau.</param>
        /// <param name="ct">Jeton d'annulation pour l'opération asynchrone.</param>
        public Task<Track?> ExecuteByIdAsync(Guid trackId, CancellationToken ct = default);
    }
}
