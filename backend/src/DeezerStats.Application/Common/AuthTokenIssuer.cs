using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Domain.Aggregates.UserAggregate;

namespace DeezerStats.Application.Common
{
    /// <summary>
    /// Implémentation partagée par RegisterUserUseCase (connexion automatique après inscription),
    /// AuthenticateUserUseCase (login) et RefreshAccessTokenUseCase (rotation) : centralise la
    /// génération de l'access token ainsi que la génération + le hachage du refresh token, pour ne
    /// pas dupliquer cette logique dans chacun des trois cas d'usage.
    ///
    /// Ne committe PAS elle-même le nouveau refresh token (voir IRefreshTokenRepository.AddAsync,
    /// qui ne fait que le suivre) : c'est à l'appelant de déclencher
    /// IUnitOfWork.SaveChangesAsync, éventuellement conjointement avec ses propres autres écritures
    /// de la même transaction logique (ex. RegisterUserUseCase, qui committe la création de
    /// l'utilisateur ET l'émission du refresh token en un seul aller-retour base).
    /// </summary>
    public class AuthTokenIssuer(
        IAccessTokenGenerator accessTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository) : IAuthTokenIssuer
    {
        private readonly IAccessTokenGenerator _accessTokenGenerator = accessTokenGenerator;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator = refreshTokenGenerator;
        private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;

        public async Task<AuthTokensDto> IssueAsync(User user, CancellationToken ct = default)
        {
            AccessTokenDto accessToken = _accessTokenGenerator.Generate(user);

            var rawRefreshToken = _refreshTokenGenerator.GenerateToken();
            var refreshTokenHash = _refreshTokenGenerator.Hash(rawRefreshToken);

            var refreshToken = new RefreshToken(
                Guid.NewGuid(),
                user.Id,
                refreshTokenHash,
                DateTime.UtcNow.AddDays(AuthRules.RefreshTokenExpirationInDays));

            await _refreshTokenRepository.AddAsync(refreshToken, ct);

            var expiresInSeconds = Math.Max(0, (int)(accessToken.ExpiresAt - DateTime.UtcNow).TotalSeconds);

            return new AuthTokensDto(accessToken.Token, rawRefreshToken, expiresInSeconds);
        }
    }
}
