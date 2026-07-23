using Microsoft.Extensions.Options;

namespace DeezerStats.Infrastructure.Adapters.Search
{
    /// <summary>
    /// Valide la configuration Meilisearch au démarrage de l'application (voir
    /// AddOptions&lt;MeilisearchOptions&gt;().ValidateOnStart() dans
    /// DependencyInjection.AddInfrastructure), même pattern que
    /// <see cref="DeezerStats.Infrastructure.Adapters.Security.JwtSettingsValidator"/> :
    /// faire échouer le démarrage immédiatement et bruyamment si la configuration est absente,
    /// plutôt que de laisser l'API démarrer avec un client Meilisearch mal configuré qui échouerait
    /// silencieusement à chaque recherche.
    /// </summary>
    public class MeilisearchOptionsValidator : IValidateOptions<MeilisearchOptions>
    {
        public ValidateOptionsResult Validate(string? name, MeilisearchOptions options)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(options.Url))
            {
                errors.Add("Meilisearch:Url est obligatoire.");
            }

            if (string.IsNullOrWhiteSpace(options.MasterKey))
            {
                errors.Add("Meilisearch:MasterKey est obligatoire.");
            }

            if (string.IsNullOrWhiteSpace(options.IndexName))
            {
                errors.Add("Meilisearch:IndexName est obligatoire.");
            }

            return errors.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(errors);
        }
    }
}
