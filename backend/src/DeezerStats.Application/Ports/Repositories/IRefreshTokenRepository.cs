using DeezerStats.Domain.Aggregates.UserAggregate;

namespace DeezerStats.Application.Ports.Repositories
{
    /// <summary>
    /// Définit les opérations de persistance pour les jetons de rafraîchissement (<see cref="RefreshToken"/>).
    /// </summary>
    public interface IRefreshTokenRepository
    {
        /// <summary>
        /// Récupère un jeton de rafraîchissement par son hash.
        /// </summary>
        /// <param name="tokenHash">Hash du jeton de rafraîchissement (valeur stockée en base).</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Le jeton correspondant, ou null s'il n'existe pas.</returns>
        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

        /// <summary>
        /// Ajoute un nouveau jeton de rafraîchissement dans la base de données.
        /// </summary>
        /// <param name="refreshToken">Jeton à ajouter.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default);

        /// <summary>
        /// Met à jour un jeton de rafraîchissement existant dans la base de données.
        /// </summary>
        /// <param name="refreshToken">Jeton contenant les données modifiées.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct = default);

        /// <summary>
        /// Révoque tous les refresh tokens actifs de l'utilisateur : réponse défensive à la
        /// réutilisation détectée d'un token déjà révoqué (vol probable), voir
        /// RefreshAccessTokenUseCase.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur dont les tokens doivent être révoqués.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken ct = default);
    }
}
