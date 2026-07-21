using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

namespace DeezerStats.Api.Controllers;

/// <summary>
/// Base commune aux contrôleurs protégés par authentification JWT (voir le FallbackPolicy global
/// dans Program.cs) : factorise l'extraction de l'identifiant utilisateur depuis le token, jusque-là
/// dupliquée dans <see cref="ImportsController"/>.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Extrait l'identifiant de l'utilisateur authentifié depuis la claim "sub" du token JWT.
    /// </summary>
    /// <returns>L'identifiant de l'utilisateur authentifié.</returns>
    /// <exception cref="InvalidOperationException">
    /// Levée si la claim est absente ou n'est pas un GUID valide : ne devrait jamais se produire
    /// derrière le FallbackPolicy (RequireAuthenticatedUser), un token invalide étant déjà rejeté
    /// en amont par le middleware d'authentification.
    /// </exception>
    protected Guid GetAuthenticatedUserId()
    {
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(subject, out Guid userId))
        {
            throw new InvalidOperationException(
                "Le token JWT ne contient pas d'identifiant utilisateur valide.");
        }

        return userId;
    }
}
