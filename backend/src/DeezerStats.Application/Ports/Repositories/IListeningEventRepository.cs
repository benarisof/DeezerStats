using DeezerStats.Domain.Aggregates.ListeningEventAggregate;

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
        /// Vérifie si un événement d'écoute existe déjà pour un utilisateur, un morceau et une date
        /// donnés. Identifié par <paramref name="trackId"/> (et non par ISRC) : TrackId est l'unique
        /// clé étrangère portée par <see cref="ListeningEvent"/>, il n'y a plus de copie de l'ISRC
        /// dessus (voir ListeningEvent.TrackId).
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur.</param>
        /// <param name="trackId">Identifiant du morceau écouté.</param>
        /// <param name="listenedAt">Date d'écoute à vérifier.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>True si l'écoute existe déjà, sinon False.</returns>
        public Task<bool> ExistsAsync(Guid userId, Guid trackId, DateTime listenedAt, CancellationToken ct = default);

        /// <summary>
        /// Pour un utilisateur donné, récupère en une seule fois les dates d'écoute déjà
        /// enregistrées pour chacun des identifiants de morceau fournis. Pensé pour les imports en
        /// lot (ex. ~50 000 lignes), afin d'éviter un aller-retour base par ligne comme le ferait
        /// <see cref="ExistsAsync"/> appelé en boucle : le résultat permet de vérifier en mémoire,
        /// pour chaque ligne du fichier, si le couple (morceau, date d'écoute) existe déjà.
        /// Regroupé par <see cref="Guid"/> (TrackId) plutôt que par ISRC : un import ne peut de toute
        /// façon produire un doublon en base que pour un morceau déjà existant (un morceau tout juste
        /// créé par cet import n'a par définition aucune écoute antérieure), donc seuls les TrackId
        /// des morceaux déjà connus ont besoin d'être interrogés ici.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur.</param>
        /// <param name="trackIds">Identifiants des morceaux à rechercher.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Dictionnaire associant chaque TrackId à l'ensemble des dates d'écoute déjà enregistrées pour cet utilisateur.</returns>
        public Task<IReadOnlyDictionary<Guid, HashSet<DateTime>>> GetExistingListenedAtsAsync(
            Guid userId,
            IEnumerable<Guid> trackIds,
            CancellationToken ct = default);
    }
}
