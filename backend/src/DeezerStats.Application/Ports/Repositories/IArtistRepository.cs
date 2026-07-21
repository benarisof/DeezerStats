using DeezerStats.Domain.Entities;

namespace DeezerStats.Application.Ports.Repositories
{
    /// <summary>
    /// Définit les opérations de persistance pour les artistes.
    /// </summary>
    public interface IArtistRepository
    {
        /// <summary>
        /// Récupère un artiste par son identifiant unique.
        /// </summary>
        /// <param name="id">Identifiant de l'artiste.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>L'artiste correspondant, ou null s'il n'existe pas.</returns>
        public Task<Artist?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Recherche un artiste par son nom (comparaison insensible à la casse et aux espaces
        /// superflus, via le nom normalisé). Permet d'éviter de créer un doublon lors de l'import
        /// du catalogue.
        /// </summary>
        /// <param name="name">Nom de l'artiste à rechercher.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>L'artiste correspondant, ou null s'il n'existe pas.</returns>
        public Task<Artist?> GetByNameAsync(string name, CancellationToken ct = default);

        /// <summary>
        /// Recherche en une seule fois tous les artistes déjà connus parmi une liste de noms.
        /// Pensé pour les imports en lot (ex. ~50 000 lignes), afin d'éviter un aller-retour base
        /// par ligne comme le ferait <see cref="GetByNameAsync"/> appelé en boucle.
        /// </summary>
        /// <param name="names">Noms d'artistes à rechercher.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Les artistes trouvés (peut être une liste vide ou partielle).</returns>
        public Task<IReadOnlyList<Artist>> GetByNamesAsync(IEnumerable<string> names, CancellationToken ct = default);

        /// <summary>
        /// Ajoute un nouvel artiste dans la base de données.
        /// </summary>
        /// <param name="artist">Artiste à ajouter.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public Task AddAsync(Artist artist, CancellationToken ct = default);

        /// <summary>
        /// Ajoute plusieurs artistes au suivi du contexte SANS déclencher la persistance :
        /// contrairement à <see cref="AddAsync"/>, l'appelant doit explicitement déclencher
        /// <see cref="DeezerStats.Application.Ports.IUnitOfWork.SaveChangesAsync"/> pour que ces
        /// artistes soient réellement écrits en base. Permet de committer plusieurs types
        /// d'entités (artistes, albums, morceaux, écoutes) en une seule transaction atomique
        /// plutôt qu'un aller-retour base par entité.
        /// </summary>
        /// <param name="artists">Artistes à ajouter.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public Task AddRangeAsync(IEnumerable<Artist> artists, CancellationToken ct = default);
    }
}
