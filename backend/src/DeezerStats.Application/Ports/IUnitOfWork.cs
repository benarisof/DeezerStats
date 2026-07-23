namespace DeezerStats.Application.Ports
{
    /// <summary>
    /// Représente une transaction applicative : permet à un cas d'usage qui manipule plusieurs
    /// agrégats (via plusieurs repositories) de committer l'ensemble en une seule fois, de façon
    /// atomique, au lieu que chaque repository sauvegarde individuellement ses propres changements.
    /// Toutes les méthodes d'écriture des repositories (AddAsync/UpdateAsync/AddRangeAsync) ne font
    /// que suivre les changements localement (aucun accès base) : c'est l'appel explicite à
    /// <see cref="SaveChangesAsync"/> par le use case qui déclenche la persistance réelle, en une
    /// seule transaction implicite EF Core -- pas besoin d'une transaction explicite
    /// (BeginTransaction/Commit/Rollback) pour committer plusieurs écritures ensemble, un seul
    /// SaveChangesAsync suffit déjà à les rendre atomiques entre elles.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Enregistre toutes les modifications apportées dans le contexte de la base de données.
        /// </summary>
        /// <param name="ct">Un jeton d'annulation pour propager la notification que l'opération doit être annulée.</param>
        /// <returns>Le nombre d'objets écrits dans la base de données.</returns>
        public Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
