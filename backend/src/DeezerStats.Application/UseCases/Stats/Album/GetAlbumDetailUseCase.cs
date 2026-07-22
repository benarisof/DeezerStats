using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Mappers;
using DeezerStats.Application.Ports.ExternalServices.Search;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Application.UseCases.Albums;
using DeezerStats.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using DomainAlbum = DeezerStats.Domain.Aggregates.AlbumAggregate.Album;

namespace DeezerStats.Application.UseCases.Stats.Album
{
    /// <summary>
    /// Enrichit l'album à la demande (stratégie cache-first, voir GetOrEnrichAlbumUseCase) avant de
    /// lire ses statistiques : l'import ne déclenche plus aucun enrichissement Deezer (voir
    /// ImportListeningHistoryUseCase), c'est donc la première consultation du détail d'un album qui
    /// s'en charge -- une seule fois par album, les consultations suivantes lisent le cache.
    /// </summary>
    public class GetAlbumDetailUseCase(
        IListeningStatsQueryPort statsQueryPort,
        IGetOrEnrichAlbumUseCase getOrEnrichAlbumUseCase,
        ISearchEnginePort searchEnginePort,
        ILogger<GetAlbumDetailUseCase> logger) : IGetAlbumDetailUseCase
    {
        private static readonly Action<ILogger, Guid, Exception?> _logIndexingError =
            LoggerMessage.Define<Guid>(
                LogLevel.Error,
                new EventId(3002, "AlbumDetailReindexError"),
                "Échec de la ré-indexation de l'album {AlbumId} après enrichissement à la demande.");

        private readonly IListeningStatsQueryPort _statsQueryPort = statsQueryPort;
        private readonly IGetOrEnrichAlbumUseCase _getOrEnrichAlbumUseCase = getOrEnrichAlbumUseCase;
        private readonly ISearchEnginePort _searchEnginePort = searchEnginePort;
        private readonly ILogger<GetAlbumDetailUseCase> _logger = logger;

        public async Task<AlbumDetail?> ExecuteAsync(GetAlbumDetailQuery query, CancellationToken ct = default)
        {
            DomainAlbum? album = await _getOrEnrichAlbumUseCase.ExecuteAsync(new GetOrEnrichAlbumRequest(query.AlbumId), ct);

            if (album is null)
            {
                return null;
            }

            var dateRange = new DateRange(query.From, query.To);
            AlbumDetail? detail = await _statsQueryPort.GetAlbumDetailAsync(query.UserId, query.AlbumId, dateRange, ct);

            if (detail is not null)
            {
                // Ré-indexe systématiquement (cover potentiellement fraîche, ou index simplement pas
                // encore à jour depuis l'import -- voir ImportListeningHistoryUseCase, qui absorbe
                // déjà les pannes de recherche). Coût négligeable : Meilisearch est local, contrairement
                // à l'appel Deezer ci-dessus qui, lui, ne se refait plus une fois l'album enrichi.
                await ReindexAsync(detail, ct);
            }

            return detail;
        }

        private async Task ReindexAsync(AlbumDetail detail, CancellationToken ct)
        {
            try
            {
                SearchDocumentDto document = SearchMapper.ToSearchDocument(detail.Id, detail.Title, detail.ArtistName, detail.CoverUrl);
                await _searchEnginePort.IndexDocumentsAsync([document], ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logIndexingError(_logger, detail.Id, ex);
            }
        }
    }
}
