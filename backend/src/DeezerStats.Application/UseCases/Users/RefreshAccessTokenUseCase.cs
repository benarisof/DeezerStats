using DeezerStats.Application.Common;
using DeezerStats.Application.Common.Exceptions;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Domain.Aggregates.UserAggregate;
using FluentValidation;

namespace DeezerStats.Application.UseCases.Users
{
    /// <summary>
    /// Échange un refresh token valide contre un nouveau couple, avec rotation : l'ancien est
    /// révoqué dès qu'il est utilisé. Si un token déjà révoqué est présenté (signe possible d'un vol
    /// de token), tous les refresh tokens actifs de l'utilisateur sont révoqués par précaution.
    /// </summary>
    public class RefreshAccessTokenUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenGenerator refreshTokenGenerator,
        IUserRepository userRepository,
        IAuthTokenIssuer authTokenIssuer,
        IUnitOfWork unitOfWork,
        IValidator<RefreshAccessTokenCommand> validator) : IRefreshAccessTokenUseCase
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator = refreshTokenGenerator;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IAuthTokenIssuer _authTokenIssuer = authTokenIssuer;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
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
                // session plutôt que ce seul token. RevokeAllActiveForUserAsync ne committe pas
                // elle-même, d'où le SaveChangesAsync explicite avant de lever l'exception.
                await _refreshTokenRepository.RevokeAllActiveForUserAsync(existingToken.UserId, ct);
                await _unitOfWork.SaveChangesAsync(ct);
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

            // Révocation de l'ancien token + émission du nouveau, persistées ensemble par ce seul
            // SaveChangesAsync : la rotation est atomique sans transaction explicite.
            await _unitOfWork.SaveChangesAsync(ct);

            return newTokens;
        }
    }
}
