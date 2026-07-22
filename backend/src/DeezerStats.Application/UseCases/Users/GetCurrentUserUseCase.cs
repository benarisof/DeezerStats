using DeezerStats.Application.Common.Exceptions;
using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Aggregates.UserAggregate;

namespace DeezerStats.Application.UseCases.Users
{
    /// <summary>
    /// Profil de l'utilisateur authentifié (GET /auth/me), utilisé par le front pour restaurer la
    /// session au chargement de l'application.
    /// </summary>
    public class GetCurrentUserUseCase(IUserRepository userRepository) : IGetCurrentUserUseCase
    {
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<UserProfileDto> ExecuteAsync(Guid userId, CancellationToken ct = default)
        {
            // Un access token valide dont l'utilisateur n'existe plus (compte supprimé après
            // émission du token) doit être traité comme une session invalide (401), pas comme une
            // 404 : voir contrat OpenAPI, GET /auth/me ne documente que 200/401.
            User user = await _userRepository.GetByIdAsync(userId, ct)
                ?? throw new AuthenticationFailedException("Utilisateur introuvable.");

            return new UserProfileDto(user.Id, user.Email.Value, user.DisplayName);
        }
    }
}
