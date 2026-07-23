namespace DeezerStats.Infrastructure.Adapters.Search
{
    public class MeilisearchOptions
    {
        public const string SectionName = "Meilisearch";

        /// <summary>
        /// Valeur de développement présente dans appsettings.json. Program.cs refuse de démarrer
        /// avec cette valeur en dehors de l'environnement Development (même garde-fou que
        /// JwtSettings.Key, voir JwtSettings.DevelopmentPlaceholderKey) : une master key par
        /// ailleurs valide structurellement (non vide) mais publique dans le dépôt ne doit jamais
        /// protéger un index Meilisearch en production.
        /// </summary>
        public const string DevelopmentPlaceholderMasterKey = "CHANGE_ME_IN_PRODUCTION";

        public string Url { get; set; } = string.Empty;

        public string MasterKey { get; set; } = string.Empty;

        public string IndexName { get; set; } = "catalog";
    }
}
