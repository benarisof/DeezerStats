using DeezerStats.Domain.Aggregates.TrackAggregate;

namespace DeezerStats.Application.UseCases.Tracks
{
    public interface IGetOrEnrichTrackUseCase
    {
        public Task<Track?> ExecuteAsync(GetOrEnrichTrackRequest request, CancellationToken ct = default);

        // Variante par identifiant plutôt que par ISRC, pour l'appelant qui connaît déjà le morceau
        // en base (ex. CatalogEnrichmentCoordinator, qui part des résultats déjà paginés d'un top).
        public Task<Track?> ExecuteByIdAsync(Guid trackId, CancellationToken ct = default);
    }
}
