using Microsoft.Extensions.Options;

namespace DeezerStats.Infrastructure.Adapters.Security
{
    /// <summary>
    /// Valide la configuration JWT au démarrage de l'application (voir
    /// AddOptions&lt;JwtSettings&gt;().ValidateOnStart() dans DependencyInjection.AddInfrastructure).
    /// Objectif : faire échouer le démarrage immédiatement et bruyamment si la configuration est
    /// absente ou invalide, plutôt que de laisser l'API démarrer sans authentification. L'ancien
    /// code de Program.cs se contentait d'un "if (jwtSettings != null)" autour de
    /// AddAuthentication/FallbackPolicy, ce qui aurait silencieusement désactivé toute
    /// authentification si la section "Jwt" venait à manquer — le même genre de bug de nommage de
    /// variable d'environnement que celui déjà rencontré avec Jwt__Secret vs Jwt__Key.
    /// </summary>
    public class JwtSettingsValidator : IValidateOptions<JwtSettings>
    {
        private const int _minimumKeyLength = 32;

        public ValidateOptionsResult Validate(string? name, JwtSettings options)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(options.Key))
            {
                errors.Add("Jwt:Key est obligatoire.");
            }
            else if (options.Key.Length < _minimumKeyLength)
            {
                errors.Add($"Jwt:Key doit contenir au moins {_minimumKeyLength} caractères (HMAC-SHA256).");
            }

            if (string.IsNullOrWhiteSpace(options.Issuer))
            {
                errors.Add("Jwt:Issuer est obligatoire.");
            }

            if (string.IsNullOrWhiteSpace(options.Audience))
            {
                errors.Add("Jwt:Audience est obligatoire.");
            }

            if (options.ExpirationInMinutes <= 0)
            {
                errors.Add("Jwt:ExpirationInMinutes doit être strictement positif.");
            }

            return errors.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(errors);
        }
    }
}
