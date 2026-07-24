using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.Repositories
{
    /// <summary>
    /// Définit les opérations de persistance pour les morceaux (tracks). AddAsync/AddRangeAsync/
    /// UpdateAsync ne déclenchent pas la persistance : l'appelant doit explicitement appeler
    /// <see cref="DeezerStats.Application.Ports.IUnitOfWork.SaveChangesAsync"/>, pour pouvoir
    /// committer plusieurs types d'entités (artistes, albums, morceaux, écoutes) en une seule
    /// transaction atomique.
    /// </summary>
    public interface ITrackRepository
    {
        public Task<Track?> GetByIdAsync(Guid id, CancellationToken ct = default);

        public Task<Track?> GetByIsrcAsync(Isrc isrc, CancellationToken ct = default);

        // Variante en lot de GetByIsrcAsync, pour éviter un aller-retour base par ligne lors des imports (~50 000 lignes).
        public Task<IReadOnlyList<Track>> GetByIsrcsAsync(IEnumerable<Isrc> isrcs, CancellationToken ct = default);

        public Task AddAsync(Track track, CancellationToken ct = default);

        public Task AddRangeAsync(IEnumerable<Track> tracks, CancellationToken ct = default);

        public Task UpdateAsync(Track track, CancellationToken ct = default);
    }
}
