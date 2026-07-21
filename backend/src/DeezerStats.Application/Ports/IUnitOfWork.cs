namespace DeezerStats.Application.Ports
{
    /// <summary>
    /// Représente une transaction applicative : permet à un cas d'usage qui manipule plusieurs
    /// agrégats (via plusieurs repositories) de committer l'ensemble en une seule fois, de façon
    /// atomique, au lieu que chaque repository sauvegarde individuellement ses propres changements.
    /// Utilisé notamment par <see cref="UseCases.Imports.ImportListeningHistoryUseCase"/> : les
    /// repositories y exposent des méthodes "AddRangeAsync" qui ne font que suivre les nouvelles
    /// entités (aucun accès base), et c'est l'appel unique à <see cref="SaveChangesAsync"/> en fin
    /// de cas d'usage qui déclenche la persistance de tout le lot dans une seule transaction.
    /// </summary>
    public interface IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
