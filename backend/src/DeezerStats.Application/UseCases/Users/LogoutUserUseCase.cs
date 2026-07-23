using DeezerStats.Application.Ports;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.Ports.Security;
using DeezerStats.Domain.Aggregates.UserAggregate;
using FluentValidation;

namespace DeezerStats.Application.UseCases.Users
{
    /// <summary>
    /// Révoque le refresh token courant de l'utilisateur authentifié. Volontairement idempotent et
    /// silencieux (jamais d'erreur) si le token est introuvable, déjà révoqué, ou n'appartient pas à
    /// l'utilisateur courant : un logout doit toujours réussir côté client, quel que soit l'état
    /// réel du token côté serveur (voir contrat OpenAPI, POST /auth/logout → 204 uniquement).
    /// </summary>
    public class LogoutUserUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenGenerator refreshTokenGenerator,
        IUnitOfWork unitOfWork,
        IValidator<LogoutUserCommand> validator) : ILogoutUserUseCase
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator = refreshTokenGenerator;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IValidator<LogoutUserCommand> _validator = validator;

        public async Task ExecuteAsync(LogoutUserCommand command, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(command, ct);

            var tokenHash = _refreshTokenGenerator.Hash(command.RefreshToken);

            RefreshToken? existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);

            if (existingToken is null || existingToken.UserId != command.UserId)
            {
                return;
            }

            existingToken.Revoke();
            await _refreshTokenRepository.UpdateAsync(existingToken, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
