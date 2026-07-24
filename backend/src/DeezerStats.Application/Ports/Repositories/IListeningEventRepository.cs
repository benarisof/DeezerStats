using DeezerStats.Domain.Aggregates.ListeningEventAggregate;

namespace DeezerStats.Application.Ports.Repositories
{
    /// <summary>
    /// Définit les opérations de persistance pour les événements d'écoute. AddRangeAsync ne
    /// déclenche pas la persistance : l'appelant doit explicitement appeler
    /// <see cref="DeezerStats.Application.Ports.IUnitOfWork.SaveChangesAsync"/>, pour pouvoir
    /// committer plusieurs types d'entités (artistes, albums, morceaux, écoutes) en une seule
    /// transaction atomique.
    /// </summary>
    public interface IListeningEventRepository
    {
        public Task AddRangeAsync(IEnumerable<ListeningEvent> events, CancellationToken ct = default);

        public Task<bool> ExistsAsync(Guid userId, Guid trackId, DateTime listenedAt, CancellationToken ct = default);

        // Variante en lot d'ExistsAsync, pour éviter un aller-retour base par ligne lors des imports (~50 000 lignes) :
        // seuls les morceaux déjà connus peuvent avoir un doublon, un morceau tout juste créé par l'import n'a par
        // définition aucune écoute antérieure.
        public Task<IReadOnlyDictionary<Guid, HashSet<DateTime>>> GetExistingListenedAtsAsync(
            Guid userId,
            IEnumerable<Guid> trackIds,
            CancellationToken ct = default);
    }
}
