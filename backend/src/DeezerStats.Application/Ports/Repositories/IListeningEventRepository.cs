using DeezerStats.Domain.Entities;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.Repositories
{
    /// <summary>
    /// Définit les opérations de persistance pour les événements d'écoute.
    /// </summary>
    public interface IListeningEventRepository
    {
        /// <summary>
        /// Ajoute plusieurs événements d'écoute au suivi du contexte SANS déclencher la
        /// persistance : l'appelant doit explicitement déclencher
        /// <see cref="DeezerStats.Application.Ports.IUnitOfWork.SaveChangesAsync"/> pour que ces
        /// écoutes soient réellement écrites en base. Permet de committer plusieurs types
        /// d'entités (artistes, albums, morceaux, écoutes) en une seule transaction atomique.
        /// </summary>
        /// <param name="events">Énumération des événements d'écoute à ajouter.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public Task AddRangeAsync(IEnumerable<ListeningEvent> events, CancellationToken ct = default);

        /// <summary>
        /// Vérifie si un événement d'écoute existe déjà pour un utilisateur, un ISRC et une date donnés.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur.</param>
        /// <param name="isrc">Code ISRC du morceau.</param>
        /// <param name="listenedAt">Date d'écoute à vérifier.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>True si l'écoute existe déjà, sinon False.</returns>
        public Task<bool> ExistsAsync(Guid userId, Isrc isrc, DateTime listenedAt, CancellationToken ct = default);

        /// <summary>
        /// Pour un utilisateur donné, récupère en une seule fois les dates d'écoute déjà
        /// enregistrées pour chacun des ISRC fournis. Pensé pour les imports en lot (ex.
        /// ~50 000 lignes), afin d'éviter un aller-retour base par ligne comme le ferait
        /// <see cref="ExistsAsync"/> appelé en boucle : le résultat permet de vérifier en mémoire,
        /// pour chaque ligne du fichier, si le couple (isrc, date d'écoute) existe déjà.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur.</param>
        /// <param name="isrcs">Liste des codes ISRC à rechercher.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Dictionnaire associant chaque ISRC à l'ensemble des dates d'écoute déjà enregistrées pour cet utilisateur.</returns>
        public Task<IReadOnlyDictionary<Isrc, HashSet<DateTime>>> GetExistingListenedAtsAsync(
            Guid userId,
            IEnumerable<Isrc> isrcs,
            CancellationToken ct = default);
    }
}
