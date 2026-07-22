using DeezerStats.Application.DTOs;
using DeezerStats.Domain.Aggregates.UserAggregate;

namespace DeezerStats.Application.Common
{
    /// <summary>
    /// Émet un couple (access token, refresh token) pour un utilisateur. Interface séparée de
    /// l'implémentation (voir AuthTokenIssuer) uniquement pour simplifier les tests des use cases
    /// appelants (Register/Authenticate/Refresh) — ce n'est pas un port vers un système externe,
    /// juste un service applicatif qui compose IAccessTokenGenerator et IRefreshTokenGenerator.
    /// </summary>
    public interface IAuthTokenIssuer
    {
        public Task<AuthTokensDto> IssueAsync(User user, CancellationToken ct = default);
    }
}
