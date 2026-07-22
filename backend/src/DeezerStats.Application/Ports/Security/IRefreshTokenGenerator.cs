namespace DeezerStats.Application.Ports.Security
{
    /// <summary>
    /// Génère et hache les jetons de rafraîchissement (refresh tokens).
    /// </summary>
    public interface IRefreshTokenGenerator
    {
        /// <summary>
        /// Génère une valeur opaque, aléatoire et cryptographiquement sûre, destinée à être envoyée
        /// telle quelle au client (jamais stockée en clair côté serveur, voir <see cref="Hash"/>).
        /// </summary>
        /// <returns>Le refresh token brut (chaîne aléatoire) à transmettre au client.</returns>
        public string GenerateToken();

        /// <summary>
        /// Calcule le hash déterministe d'un refresh token, pour la persistance et la recherche en
        /// base (contrairement au hash de mot de passe, volontairement non salé : la valeur d'entrée
        /// est déjà à haute entropie, et une recherche par égalité exacte est nécessaire pour
        /// retrouver le token présenté lors d'un refresh).
        /// </summary>
        /// <param name="token">Le refresh token brut à hacher.</param>
        /// <returns>Le hash déterministe du token (à stocker en base).</returns>
        public string Hash(string token);
    }
}
