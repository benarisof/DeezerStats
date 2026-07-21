using DeezerStats.Application.Ports.BackgroundJobs;
using DeezerStats.Application.UseCases.Albums;
using DeezerStats.Application.UseCases.Artists;
using DeezerStats.Application.UseCases.Tracks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeezerStats.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Consomme en arrière-plan la file d'enrichissement alimentée après chaque import (voir
    /// ImportListeningHistoryUseCase), afin de ne jamais bloquer la réponse HTTP de POST /imports
    /// sur des appels réseau vers l'API Deezer (voir contrat OpenAPI de /imports).
    ///
    /// Une <see cref="IServiceScope"/> est créée pour chaque élément traité : les dépendances
    /// consommées (use cases d'enrichissement, repositories, DbContext...) sont Scoped, alors que ce
    /// BackgroundService est nécessairement Singleton (contrat IHostedService).
    ///
    /// Une erreur sur un élément (Deezer indisponible, morceau supprimé entre-temps, etc.) est
    /// journalisée puis absorbée : elle ne doit jamais interrompre le traitement des éléments
    /// suivants de la file.
    /// </summary>
    public class EnrichmentBackgroundService(
        IEnrichmentJobReader jobReader,
        IServiceScopeFactory scopeFactory,
        ILogger<EnrichmentBackgroundService> logger) : BackgroundService
    {
        // Définition du délégué LoggerMessage fortement typé
        private static readonly Action<ILogger, EnrichmentWorkItem, Exception?> _logEnrichmentError =
            LoggerMessage.Define<EnrichmentWorkItem>(
                logLevel: LogLevel.Error,
                eventId: new EventId(1002, "EnrichmentError"),
                formatString: "Échec de l'enrichissement Deezer en arrière-plan pour {WorkItem}.");

        private readonly IEnrichmentJobReader _jobReader = jobReader;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<EnrichmentBackgroundService> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (EnrichmentWorkItem item in _jobReader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessAsync(item, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logEnrichmentError(_logger, item, ex);
                }
            }
        }

        private async Task ProcessAsync(EnrichmentWorkItem item, CancellationToken ct)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();

            switch (item)
            {
                case EnrichmentWorkItem.ForTrack forTrack:
                    IGetOrEnrichTrackUseCase trackUseCase =
                        scope.ServiceProvider.GetRequiredService<IGetOrEnrichTrackUseCase>();
                    await trackUseCase.ExecuteAsync(new GetOrEnrichTrackRequest(forTrack.Isrc), ct);
                    break;

                case EnrichmentWorkItem.ForAlbum forAlbum:
                    IGetOrEnrichAlbumUseCase albumUseCase =
                        scope.ServiceProvider.GetRequiredService<IGetOrEnrichAlbumUseCase>();
                    await albumUseCase.ExecuteAsync(new GetOrEnrichAlbumRequest(forAlbum.AlbumId), ct);
                    break;

                case EnrichmentWorkItem.ForArtist forArtist:
                    IGetOrEnrichArtistUseCase artistUseCase =
                        scope.ServiceProvider.GetRequiredService<IGetOrEnrichArtistUseCase>();
                    await artistUseCase.ExecuteAsync(new GetOrEnrichArtistRequest(forArtist.ArtistId), ct);
                    break;
            }
        }
    }
}
