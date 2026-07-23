using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.Repositories
{
    /// <summary>
    /// Définit les opérations de persistance pour les morceaux (tracks).
    /// </summary>
    public interface ITrackRepository
    {
        /// <summary>
        /// Récupère un morceau par son identifiant unique.
        /// </summary>
        /// <param name="id">Identifiant du morceau.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Le morceau correspondant, ou null s'il n'existe pas.</returns>
        public Task<Track?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Récupère un morceau par son ISRC.
        /// </summary>
        /// <param name="isrc">Code ISRC du morceau.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Le morceau correspondant, ou null s'il n'existe pas.</returns>
        public Task<Track?> GetByIsrcAsync(Isrc isrc, CancellationToken ct = default);

        /// <summary>
        /// Recherche en une seule fois tous les morceaux déjà connus parmi une liste d'ISRC.
        /// Pensé pour les imports en lot (ex. ~50 000 lignes), afin d'éviter un aller-retour base
        /// par ligne comme le ferait <see cref="GetByIsrcAsync"/> appelé en boucle.
        /// </summary>
        /// <param name="isrcs">Liste des codes ISRC à rechercher.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>La liste des morceaux trouvés pour les ISRC fournis.</returns>
        public Task<IReadOnlyList<Track>> GetByIsrcsAsync(IEnumerable<Isrc> isrcs, CancellationToken ct = default);

        /// <summary>
        /// Ajoute un nouveau morceau au suivi du contexte SANS déclencher la persistance :
        /// l'appelant doit explicitement déclencher
        /// <see cref="DeezerStats.Application.Ports.IUnitOfWork.SaveChangesAsync"/> pour que ce
        /// morceau soit réellement écrit en base (même contrat que <see cref="AddRangeAsync"/>,
        /// pour ne jamais avoir à deviner si une méthode d'ajout committe ou non).
        /// </summary>
        /// <param name="track">Morceau à ajouter.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public Task AddAsync(Track track, CancellationToken ct = default);

        /// <summary>
        /// Ajoute plusieurs morceaux au suivi du contexte SANS déclencher la persistance : contrairement
        /// à <see cref="AddAsync"/>, l'appelant doit explicitement déclencher
        /// <see cref="DeezerStats.Application.Ports.IUnitOfWork.SaveChangesAsync"/> pour que ces
        /// morceaux soient réellement écrits en base. Permet de committer plusieurs types d'entités
        /// (artistes, albums, morceaux, écoutes) en une seule transaction atomique plutôt qu'un
        /// aller-retour base par entité.
        /// </summary>
        /// <param name="tracks">Énumération des morceaux à ajouter.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public Task AddRangeAsync(IEnumerable<Track> tracks, CancellationToken ct = default);

        /// <summary>
        /// Met à jour un morceau existant au suivi du contexte SANS déclencher la persistance :
        /// même contrat que <see cref="AddAsync"/>, l'appelant doit explicitement déclencher
        /// <see cref="DeezerStats.Application.Ports.IUnitOfWork.SaveChangesAsync"/>.
        /// </summary>
        /// <param name="track">Morceau contenant les données modifiées.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public Task UpdateAsync(Track track, CancellationToken ct = default);
    }
}
