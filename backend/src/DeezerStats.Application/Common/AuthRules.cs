namespace DeezerStats.Application.Common
{
    /// <summary>
    /// Règles métier transverses à l'authentification, centralisées ici pour les mêmes raisons que
    /// <see cref="StatsRules"/> : éviter qu'une constante magique ne soit dupliquée entre les
    /// use cases et les tests.
    /// </summary>
    public static class AuthRules
    {
        /// <summary>
        /// Durée de vie d'un refresh token avant expiration. Volontairement plus longue que
        /// l'access token (voir JwtSettings.ExpirationInMinutes) : c'est le mécanisme qui permet à
        /// une session de survivre à l'expiration de l'access token sans reconnexion manuelle.
        /// </summary>
        public const int RefreshTokenExpirationInDays = 30;
    }
}
