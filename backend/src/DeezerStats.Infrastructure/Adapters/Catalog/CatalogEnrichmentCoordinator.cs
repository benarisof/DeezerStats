using System.Collections.Concurrent;
using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.Mappers;
using DeezerStats.Application.Ports.Catalog;
using DeezerStats.Application.Ports.ExternalServices.Search;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.UseCases.Albums;
using DeezerStats.Application.UseCases.Artists;
using DeezerStats.Application.UseCases.Tracks;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeezerStats.Infrastructure.Adapters.Catalog
{
    /// <summary>
    /// Enrichit plusieurs éléments du catalogue en parallèle, à concurrence bornée (voir
    /// <see cref="MaxConcurrency"/>). Chaque élément est traité dans son propre <see cref="IServiceScope"/> :
    /// les use cases GetOrEnrichX et leurs dépendances (DbContext compris) sont Scoped et ne
    /// supportent pas d'être utilisées simultanément par plusieurs threads.
    ///
    /// Une erreur sur un élément (Deezer indisponible, élément supprimé entre-temps, etc.) est
    /// journalisée puis absorbée : elle ne doit jamais faire échouer l'enrichissement des autres
    /// éléments, ni la requête de liste qui a déclenché cet enrichissement.
    /// </summary>
    public class CatalogEnrichmentCoordinator(
        IServiceScopeFactory scopeFactory,
        ILogger<CatalogEnrichmentCoordinator> logger) : ICatalogEnrichmentCoordinator
    {
        private const int MaxConcurrency = 10;

        private static readonly Action<ILogger, Guid, Exception?> _logEnrichmentError =
            LoggerMessage.Define<Guid>(
                LogLevel.Error,
                new EventId(3004, "CatalogEnrichmentError"),
                "Échec de l'enrichissement à la demande de l'élément {ItemId}.");

        private static readonly Action<ILogger, int, Exception?> _logIndexingError =
            LoggerMessage.Define<int>(
                LogLevel.Error,
                new EventId(3005, "CatalogEnrichmentReindexError"),
                "Échec de la ré-indexation de {Count} élément(s) après enrichissement à la demande.");

        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<CatalogEnrichmentCoordinator> _logger = logger;

        public async Task<IReadOnlyDictionary<Guid, string?>> EnrichAlbumsAsync(IReadOnlyCollection<Guid> albumIds, CancellationToken ct = default)
        {
            if (albumIds.Count == 0)
            {
                return new Dictionary<Guid, string?>();
            }

            var freshCovers = new ConcurrentDictionary<Guid, string?>();
            var documents = new ConcurrentBag<SearchDocumentDto>();

            await Parallel.ForEachAsync(albumIds, ParallelOptionsFor(ct), async (albumId, itemCt) =>
            {
                try
                {
                    using IServiceScope scope = _scopeFactory.CreateScope();
                    var getOrEnrich = scope.ServiceProvider.GetRequiredService<IGetOrEnrichAlbumUseCase>();
                    var artistRepository = scope.ServiceProvider.GetRequiredService<IArtistRepository>();

                    Album? album = await getOrEnrich.ExecuteAsync(new GetOrEnrichAlbumRequest(albumId), itemCt);

                    if (album?.CoverUrl is null)
                    {
                        return;
                    }

                    freshCovers[albumId] = album.CoverUrl;

                    Artist? artist = await artistRepository.GetByIdAsync(album.ArtistId, itemCt);
                    documents.Add(SearchMapper.ToSearchDocument(album.Id, album.Title, artist?.Name ?? string.Empty, album.CoverUrl));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logEnrichmentError(_logger, albumId, ex);
                }
            });

            await ReindexAsync(documents, ct);
            return freshCovers;
        }

        public async Task<IReadOnlyDictionary<Guid, string?>> EnrichArtistsAsync(IReadOnlyCollection<Guid> artistIds, CancellationToken ct = default)
        {
            if (artistIds.Count == 0)
            {
                return new Dictionary<Guid, string?>();
            }

            var freshCovers = new ConcurrentDictionary<Guid, string?>();
            var documents = new ConcurrentBag<SearchDocumentDto>();

            await Parallel.ForEachAsync(artistIds, ParallelOptionsFor(ct), async (artistId, itemCt) =>
            {
                try
                {
                    using IServiceScope scope = _scopeFactory.CreateScope();
                    var getOrEnrich = scope.ServiceProvider.GetRequiredService<IGetOrEnrichArtistUseCase>();

                    Artist? artist = await getOrEnrich.ExecuteAsync(new GetOrEnrichArtistRequest(artistId), itemCt);

                    if (artist?.CoverUrl is null)
                    {
                        return;
                    }

                    freshCovers[artistId] = artist.CoverUrl;
                    documents.Add(SearchMapper.ToSearchDocument(artist.Id, artist.Name, artist.CoverUrl));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logEnrichmentError(_logger, artistId, ex);
                }
            });

            await ReindexAsync(documents, ct);
            return freshCovers;
        }

        public async Task<IReadOnlyDictionary<Guid, string?>> EnrichTracksAsync(IReadOnlyCollection<Guid> trackIds, CancellationToken ct = default)
        {
            if (trackIds.Count == 0)
            {
                return new Dictionary<Guid, string?>();
            }

            var freshCovers = new ConcurrentDictionary<Guid, string?>();
            var documents = new ConcurrentBag<SearchDocumentDto>();

            await Parallel.ForEachAsync(trackIds, ParallelOptionsFor(ct), async (trackId, itemCt) =>
            {
                try
                {
                    using IServiceScope scope = _scopeFactory.CreateScope();
                    var getOrEnrich = scope.ServiceProvider.GetRequiredService<IGetOrEnrichTrackUseCase>();
                    var artistRepository = scope.ServiceProvider.GetRequiredService<IArtistRepository>();

                    Track? track = await getOrEnrich.ExecuteByIdAsync(trackId, itemCt);

                    if (track?.CoverUrl is null)
                    {
                        return;
                    }

                    freshCovers[trackId] = track.CoverUrl;

                    Artist? artist = await artistRepository.GetByIdAsync(track.ArtistId, itemCt);
                    documents.Add(SearchMapper.ToSearchDocumentForTrack(track.Id, track.Title, artist?.Name ?? string.Empty, track.CoverUrl));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logEnrichmentError(_logger, trackId, ex);
                }
            });

            await ReindexAsync(documents, ct);
            return freshCovers;
        }

        private static ParallelOptions ParallelOptionsFor(CancellationToken ct) => new()
        {
            MaxDegreeOfParallelism = MaxConcurrency,
            CancellationToken = ct,
        };

        private async Task ReindexAsync(ConcurrentBag<SearchDocumentDto> documents, CancellationToken ct)
        {
            if (documents.IsEmpty)
            {
                return;
            }

            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                var searchEnginePort = scope.ServiceProvider.GetRequiredService<ISearchEnginePort>();
                await searchEnginePort.IndexDocumentsAsync(documents, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logIndexingError(_logger, documents.Count, ex);
            }
        }
    }
}
