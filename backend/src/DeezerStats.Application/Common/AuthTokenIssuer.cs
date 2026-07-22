using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Domain.Aggregates.UserAggregate;

namespace DeezerStats.Application.Common
{
    /// <summary>
    /// Implémentation partagée par RegisterUserUseCase (connexion automatique après inscription),
    /// AuthenticateUserUseCase (login) et RefreshAccessTokenUseCase (rotation) : centralise la
    /// génération de l'access token, la génération + le hachage + la persistance du refresh token,
    /// pour ne pas dupliquer cette logique dans chacun des trois cas d'usage.
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
