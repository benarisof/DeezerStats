using Meilisearch;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeezerStats.Infrastructure.Adapters.Search;

/// <summary>
/// Service d'initialisation pour configurer l'index Meilisearch au démarrage de l'application.
/// </summary>
public partial class MeilisearchInitializerService(
    MeilisearchClient client,
    IOptions<MeilisearchOptions> options,
    ILogger<MeilisearchInitializerService> logger) : IHostedService
{
    private readonly MeilisearchOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogCheckingIndex(logger, _options.IndexName);

        try
        {
            Meilisearch.Index index = client.Index(_options.IndexName);

            await index.UpdateSearchableAttributesAsync(["label", "subtitle"], cancellationToken);
            await index.UpdateFilterableAttributesAsync(["type"], cancellationToken);

            // Configuration de la tolérance aux fautes de frappe avec les valeurs par défaut
            var typoSettings = new TypoTolerance
            {
                Enabled = true,
            };
            await index.UpdateTypoToleranceAsync(typoSettings, cancellationToken);

            LogConfigurationSuccess(logger);
        }
        catch (Exception ex)
        {
            LogConfigurationFailed(logger, ex);
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Vérification et configuration de l'index Meilisearch '{IndexName}'...")]
    private static partial void LogCheckingIndex(ILogger logger, string indexName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Configuration de l'index Meilisearch terminée avec succès.")]
    private static partial void LogConfigurationSuccess(ILogger logger);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Critical,
        Message = "Échec lors de la configuration de l'index Meilisearch. Le moteur de recherche pourrait ne pas fonctionner correctement.")]
    private static partial void LogConfigurationFailed(ILogger logger, Exception ex);
}
