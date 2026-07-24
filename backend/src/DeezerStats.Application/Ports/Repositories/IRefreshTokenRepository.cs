using DeezerStats.Domain.Aggregates.UserAggregate;

namespace DeezerStats.Application.Ports.Repositories
{
    /// <summary>
    /// Définit les opérations de persistance pour les jetons de rafraîchissement (<see cref="RefreshToken"/>).
    /// AddAsync/UpdateAsync/RevokeAllActiveForUserAsync ne déclenchent pas la persistance : l'appelant
    /// doit explicitement appeler <see cref="DeezerStats.Application.Ports.IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    public interface IRefreshTokenRepository
    {
        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

        public Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default);

        public Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct = default);

        // Réponse défensive à la réutilisation détectée d'un token déjà révoqué (vol probable) : voir RefreshAccessTokenUseCase.
        public Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken ct = default);
    }
}
