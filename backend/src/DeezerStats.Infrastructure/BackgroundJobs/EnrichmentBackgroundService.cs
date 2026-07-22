using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.Mappers;
using DeezerStats.Application.Ports.BackgroundJobs;
using DeezerStats.Application.Ports.ExternalServices.Search;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.UseCases.Albums;
using DeezerStats.Application.UseCases.Artists;
using DeezerStats.Application.UseCases.Tracks;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
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
    /// consommées (use cases d'enrichissement, repositories, DbContext, moteur de recherche...)
    /// sont Scoped, alors que ce BackgroundService est nécessairement Singleton (contrat IHostedService).
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

            // On récupère le port de recherche depuis le scope (Scoped)
            ISearchEnginePort searchEnginePort = scope.ServiceProvider.GetRequiredService<ISearchEnginePort>();

            switch (item)
            {
                case EnrichmentWorkItem.ForTrack forTrack:
                    IGetOrEnrichTrackUseCase trackUseCase = scope.ServiceProvider.GetRequiredService<IGetOrEnrichTrackUseCase>();
                    Track? trackResult = await trackUseCase.ExecuteAsync(new GetOrEnrichTrackRequest(forTrack.Isrc), ct);

                    if (trackResult is null)
                    {
                        break;
                    }

                    IArtistRepository artistRepositoryForTrack = scope.ServiceProvider.GetRequiredService<IArtistRepository>();
                    Artist? trackArtist = await artistRepositoryForTrack.GetByIdAsync(trackResult.ArtistId, ct);

                    SearchDocumentDto trackDoc = SearchMapper.ToSearchDocumentForTrack(
                        trackResult.Id, trackResult.Title, trackArtist?.Name ?? string.Empty, trackResult.CoverUrl);
                    await searchEnginePort.IndexDocumentAsync(trackDoc, ct);
                    break;

                case EnrichmentWorkItem.ForAlbum forAlbum:
                    IGetOrEnrichAlbumUseCase albumUseCase = scope.ServiceProvider.GetRequiredService<IGetOrEnrichAlbumUseCase>();
                    Album? albumResult = await albumUseCase.ExecuteAsync(new GetOrEnrichAlbumRequest(forAlbum.AlbumId), ct);

                    if (albumResult is null)
                    {
                        break;
                    }

                    IArtistRepository artistRepositoryForAlbum = scope.ServiceProvider.GetRequiredService<IArtistRepository>();
                    Artist? albumArtist = await artistRepositoryForAlbum.GetByIdAsync(albumResult.ArtistId, ct);

                    SearchDocumentDto albumDoc = SearchMapper.ToSearchDocument(
                        albumResult.Id, albumResult.Title, albumArtist?.Name ?? string.Empty, albumResult.CoverUrl);
                    await searchEnginePort.IndexDocumentAsync(albumDoc, ct);
                    break;

                case EnrichmentWorkItem.ForArtist forArtist:
                    IGetOrEnrichArtistUseCase artistUseCase = scope.ServiceProvider.GetRequiredService<IGetOrEnrichArtistUseCase>();
                    Artist? artistResult = await artistUseCase.ExecuteAsync(new GetOrEnrichArtistRequest(forArtist.ArtistId), ct);

                    if (artistResult is null)
                    {
                        break;
                    }

                    SearchDocumentDto artistDoc = SearchMapper.ToSearchDocument(artistResult.Id, artistResult.Name, artistResult.CoverUrl);
                    await searchEnginePort.IndexDocumentAsync(artistDoc, ct);
                    break;
            }
        }
    }
}
