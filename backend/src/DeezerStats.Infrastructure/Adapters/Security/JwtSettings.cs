namespace DeezerStats.Infrastructure.Adapters.Security
{
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        /// <summary>
        /// Valeur de développement présente dans appsettings.json. Program.cs refuse de démarrer
        /// avec cette valeur en dehors de l'environnement Development (voir le garde-fou après
        /// builder.Build()) : une clé par ailleurs valide structurellement (longueur suffisante)
        /// mais publique dans le dépôt ne doit jamais signer de tokens en production.
        /// </summary>
        public const string DevelopmentPlaceholderKey = "CHANGE_ME_WITH_A_LONG_RANDOM_SECRET_KEY";

        public string Key { get; init; } = string.Empty;

        public string Issuer { get; init; } = string.Empty;

        public string Audience { get; init; } = string.Empty;

        public int ExpirationInMinutes { get; init; }
    }
}
