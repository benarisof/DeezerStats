using DeezerStats.Application.Common;
using DeezerStats.Application.Common.Exceptions;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Domain.Aggregates.UserAggregate;
using FluentValidation;

namespace DeezerStats.Application.UseCases.Users
{
    /// <summary>
    /// Échange un refresh token valide contre un nouveau couple (access token, refresh token), avec
    /// rotation : l'ancien refresh token est révoqué dès qu'il est utilisé (voir RefreshToken.Revoke)
    /// et ne peut donc plus resservir. Si un refresh token déjà révoqué est présenté, c'est le signe
    /// d'une possible réutilisation frauduleuse (vol de token) : par précaution, tous les refresh
    /// tokens actifs de l'utilisateur sont révoqués, forçant une reconnexion complète.
    /// </summary>
    public class RefreshAccessTokenUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenGenerator refreshTokenGenerator,
        IUserRepository userRepository,
        IAuthTokenIssuer authTokenIssuer,
        IValidator<RefreshAccessTokenCommand> validator) : IRefreshAccessTokenUseCase
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator = refreshTokenGenerator;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IAuthTokenIssuer _authTokenIssuer = authTokenIssuer;
        private readonly IValidator<RefreshAccessTokenCommand> _validator = validator;

        public async Task<AuthTokensDto> ExecuteAsync(
            RefreshAccessTokenCommand command,
            CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(command, ct);

            var tokenHash = _refreshTokenGenerator.Hash(command.RefreshToken);

            RefreshToken? existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);

            if (existingToken is null)
            {
                throw new AuthenticationFailedException("Refresh token invalide ou expiré.");
            }

            if (existingToken.IsRevoked)
            {
                // Réutilisation d'un token déjà révoqué : réponse défensive, on invalide toute la
                // session de l'utilisateur plutôt que ce seul token.
                await _refreshTokenRepository.RevokeAllActiveForUserAsync(existingToken.UserId, ct);
                throw new AuthenticationFailedException("Refresh token invalide ou expiré.");
            }

            if (existingToken.IsExpired)
            {
                throw new AuthenticationFailedException("Refresh token invalide ou expiré.");
            }

            User? user = await _userRepository.GetByIdAsync(existingToken.UserId, ct)
                ?? throw new AuthenticationFailedException("Refresh token invalide ou expiré.");

            AuthTokensDto newTokens = await _authTokenIssuer.IssueAsync(user, ct);

            existingToken.Revoke();
            await _refreshTokenRepository.UpdateAsync(existingToken, ct);

            return newTokens;
        }
    }
}
