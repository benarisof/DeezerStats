namespace DeezerStats.Application.DTOs
{
    /// <summary>
    /// Profil de l'utilisateur connecté — voir le schéma UserProfile du contrat OpenAPI
    /// (GET /auth/me, restauration de session au chargement du front).
    /// </summary>
    public record UserProfileDto(
        Guid Id,
        string Email,
        string DisplayName);
}
