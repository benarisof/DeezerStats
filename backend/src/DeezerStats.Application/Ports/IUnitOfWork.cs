namespace DeezerStats.Application.Ports
{
    /// <summary>
    /// Représente une transaction applicative : permet à un cas d'usage qui manipule plusieurs
    /// agrégats (via plusieurs repositories) de committer l'ensemble en une seule fois, de façon
    /// atomique, au lieu que chaque repository sauvegarde individuellement ses propres changements.
    /// Utilisé notamment par <see cref="UseCases.Import.ImportListeningHistoryUseCase"/> : les
    /// repositories y exposent des méthodes "AddRangeAsync" qui ne font que suivre les nouvelles
    /// entités (aucun accès base), et c'est l'appel unique à <see cref="SaveChangesAsync"/> en fin
    /// de cas d'usage qui déclenche la persistance de tout le lot dans une seule transaction.
    /// </summary>
    public interface IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default);

        /// <summary>
        /// Exécute <paramref name="operation"/> dans une transaction explicite : si l'un des appels
        /// SaveChangesAsync déclenchés par <paramref name="operation"/> échoue, tous les précédents
        /// sont annulés. Nécessaire quand un cas d'usage enchaîne plusieurs écritures indépendantes
        /// (chacune via son propre repository, donc son propre SaveChangesAsync) qui doivent réussir
        /// ou échouer ensemble (voir RegisterUserUseCase : création de l'utilisateur + émission du
        /// refresh token).
        /// </summary>
        public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct = default);
    }
}
