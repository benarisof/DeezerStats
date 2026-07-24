namespace DeezerStats.Application.Ports
{
    /// <summary>
    /// Permet à un cas d'usage qui manipule plusieurs agrégats (via plusieurs repositories) de
    /// committer l'ensemble en une seule fois. Les méthodes d'écriture des repositories
    /// (AddAsync/UpdateAsync/AddRangeAsync) ne font que suivre les changements localement : c'est
    /// l'appel explicite à SaveChangesAsync qui déclenche la persistance réelle, en une seule
    /// transaction implicite EF Core -- pas besoin de BeginTransaction/Commit/Rollback explicite.
    /// </summary>
    public interface IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
