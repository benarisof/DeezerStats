using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.Repositories
{
    public interface ITrackRepository
    {
        public Task<Track?> GetByIdAsync(Guid id, CancellationToken ct = default);

        public Task<Track?> GetByIsrcAsync(Isrc isrc, CancellationToken ct = default);

        /// <summary>
        /// Recherche en une seule fois tous les morceaux déjà connus parmi une liste d'ISRC.
        /// Pensé pour les imports en lot (ex. ~50 000 lignes), afin d'éviter un aller-retour base
        /// par ligne comme le ferait <see cref="GetByIsrcAsync"/> appelé en boucle.
        /// </summary>
        public Task<IReadOnlyList<Track>> GetByIsrcsAsync(IEnumerable<Isrc> isrcs, CancellationToken ct = default);

        public Task AddAsync(Track track, CancellationToken ct = default);

        /// <summary>
        /// Ajoute plusieurs morceaux au suivi du contexte SANS déclencher la persistance : contrairement
        /// à <see cref="AddAsync"/>, l'appelant doit explicitement déclencher
        /// <see cref="DeezerStats.Application.Ports.IUnitOfWork.SaveChangesAsync"/> pour que ces
        /// morceaux soient réellement écrits en base. Permet de committer plusieurs types d'entités (artistes, albums, morceaux, écoutes)
        /// en une seule transaction atomique plutôt qu'un aller-retour base par entité.
        /// </summary>
        public Task AddRangeAsync(IEnumerable<Track> tracks, CancellationToken ct = default);

        public Task UpdateAsync(Track track, CancellationToken ct = default);
    }
}
