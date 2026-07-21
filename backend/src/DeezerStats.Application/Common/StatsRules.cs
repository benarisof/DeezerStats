namespace DeezerStats.Application.Common
{
    /// <summary>
    /// Règles métier transverses aux statistiques d'écoute (tops, historique). Centralisées ici
    /// pour éviter que la même constante magique ("100") ne soit dupliquée entre la validation des
    /// requêtes (pageSize maximum) et l'implémentation du port de lecture (plafond du classement).
    /// </summary>
    public static class StatsRules
    {
        /// <summary>
        /// Nombre maximal d'éléments classés exposés par les tops (albums/artistes/morceaux) et par
        /// l'historique, quel que soit le volume réel de données : le classement complet n'est
        /// jamais paginé au-delà de ce plafond (voir openapi.yaml, paramètre PageSize : "Le total
        /// est plafonné par la règle métier").
        /// </summary>
        public const int MaxRankedResults = 100;
    }
}
