using DeezerStats.Domain.Entities;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.Repositories
{
    public interface IListeningEventRepository
    {
        /// <summary>
        /// Ajoute plusieurs événements d'écoute au suivi du contexte SANS déclencher la
        /// persistance : l'appelant doit explicitement déclencher
        /// <see cref="DeezerStats.Application.Ports.IUnitOfWork.SaveChangesAsync"/> pour que ces
        /// écoutes soient réellement écrites en base. Permet de committer plusieurs types
        /// d'entités (artistes, albums, morceaux, écoutes) en une seule transaction atomique.
        /// </summary>
        public Task AddRangeAsync(IEnumerable<ListeningEvent> events, CancellationToken ct = default);

        public Task<bool> ExistsAsync(Guid userId, Isrc isrc, DateTime listenedAt, CancellationToken ct = default);

        /// <summary>
        /// Pour un utilisateur donné, récupère en une seule fois les dates d'écoute déjà
        /// enregistrées pour chacun des ISRC fournis. Pensé pour les imports en lot (ex.
        /// ~50 000 lignes), afin d'éviter un aller-retour base par ligne comme le ferait
        /// <see cref="ExistsAsync"/> appelé en boucle : le résultat permet de vérifier en mémoire,
        /// pour chaque ligne du fichier, si le couple (isrc, date d'écoute) existe déjà.
        /// </summary>
        public Task<IReadOnlyDictionary<Isrc, HashSet<DateTime>>> GetExistingListenedAtsAsync(
            Guid userId,
            IEnumerable<Isrc> isrcs,
            CancellationToken ct = default);
    }
}
