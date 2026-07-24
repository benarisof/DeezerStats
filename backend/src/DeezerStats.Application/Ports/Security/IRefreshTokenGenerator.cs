namespace DeezerStats.Application.Ports.Security
{
    public interface IRefreshTokenGenerator
    {
        // Valeur opaque, aléatoire et cryptographiquement sûre, envoyée telle quelle au client
        // (jamais stockée en clair côté serveur, voir Hash).
        public string GenerateToken();

        // Hash déterministe (contrairement au hash de mot de passe, volontairement non salé : la
        // valeur d'entrée est déjà à haute entropie, et une recherche par égalité exacte est
        // nécessaire pour retrouver le token présenté lors d'un refresh).
        public string Hash(string token);
    }
}
