using DeezerStats.Domain.Aggregates.UserAggregate;

namespace DeezerStats.Application.Ports.Repositories
{
    public interface IRefreshTokenRepository
    {
        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

        public Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default);

        public Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct = default);

        /// <summary>
        /// Révoque tous les refresh tokens actifs de l'utilisateur : réponse défensive à la
        /// réutilisation détectée d'un token déjà révoqué (vol probable), voir
        /// RefreshAccessTokenUseCase.
        /// </summary>
        public Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken ct = default);
    }
}
