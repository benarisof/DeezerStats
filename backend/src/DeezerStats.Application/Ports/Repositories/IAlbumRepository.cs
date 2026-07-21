using DeezerStats.Domain.Aggregates.AlbumAggregate;

namespace DeezerStats.Application.Ports.Repositories
{
    /// <summary>
    /// Définit les opérations de persistance pour les albums.
    /// </summary>
    public interface IAlbumRepository
    {
        /// <summary>
        /// Récupère un album par son identifiant unique.
        /// </summary>
        /// <param name="id">Identifiant de l'album.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>L'album correspondant, ou null s'il n'existe pas.</returns>
        public Task<Album?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Recherche un album par son titre pour un artiste donné (comparaison insensible à la casse
        /// et aux espaces superflus, via le titre normalisé). Permet d'éviter de créer un doublon
        /// lors de l'import du catalogue.
        /// </summary>
        /// <param name="title">Titre de l'album à rechercher.</param>
        /// <param name="artistId">Identifiant de l'artiste associé.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>L'album correspondant, ou null s'il n'existe pas.</returns>
        public Task<Album?> GetByTitleAndArtistAsync(string title, Guid artistId, CancellationToken ct = default);

        /// <summary>
        /// Recherche en une seule fois tous les albums déjà connus pour un ensemble d'artistes.
        /// Pensé pour les imports en lot (ex. ~50 000 lignes), afin d'éviter un aller-retour base
        /// par ligne comme le ferait <see cref="GetByTitleAndArtistAsync"/> appelé en boucle.
        /// </summary>
        /// <param name="artistIds">Identifiants des artistes dont on veut récupérer les albums.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Les albums trouvés (peut être une liste vide ou partielle).</returns>
        public Task<IReadOnlyList<Album>> GetByArtistIdsAsync(IEnumerable<Guid> artistIds, CancellationToken ct = default);

        /// <summary>
        /// Ajoute un nouvel album dans la base de données.
        /// </summary>
        /// <param name="album">Album à ajouter.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public Task AddAsync(Album album, CancellationToken ct = default);

        /// <summary>
        /// Ajoute plusieurs albums au suivi du contexte SANS déclencher la persistance :
        /// contrairement à <see cref="AddAsync"/>, l'appelant doit explicitement déclencher
        /// <see cref="DeezerStats.Application.Ports.IUnitOfWork.SaveChangesAsync"/> pour que ces
        /// albums soient réellement écrits en base. Permet de committer plusieurs types d'entités
        /// (artistes, albums, morceaux, écoutes) en une seule transaction atomique plutôt qu'un
        /// aller-retour base par entité.
        /// </summary>
        /// <param name="albums">Albums à ajouter.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public Task AddRangeAsync(IEnumerable<Album> albums, CancellationToken ct = default);

        /// <summary>
        /// Met à jour un album existant dans la base de données.
        /// </summary>
        /// <param name="album">Album contenant les données modifiées.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public Task UpdateAsync(Album album, CancellationToken ct = default);
    }
}
