using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Mappers;
using DeezerStats.Application.Ports.ExternalServices.Search;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Application.UseCases.Artists;
using DeezerStats.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using DomainArtist = DeezerStats.Domain.Aggregates.ArtistAggregate.Artist;

namespace DeezerStats.Application.UseCases.Stats.Artist
{
    /// <summary>
    /// Enrichit l'artiste à la demande (stratégie cache-first, voir GetOrEnrichArtistUseCase) avant
    /// de lire ses statistiques : l'import ne déclenche plus aucun enrichissement Deezer (voir
    /// ImportListeningHistoryUseCase), c'est donc la première consultation du détail d'un artiste qui
    /// s'en charge -- une seule fois par artiste, les consultations suivantes lisent le cache.
    /// </summary>
    public class GetArtistDetailUseCase(
        IListeningStatsQueryPort statsQueryPort,
        IGetOrEnrichArtistUseCase getOrEnrichArtistUseCase,
        ISearchEnginePort searchEnginePort,
        ILogger<GetArtistDetailUseCase> logger) : IGetArtistDetailUseCase
    {
        private static readonly Action<ILogger, Guid, Exception?> _logIndexingError =
            LoggerMessage.Define<Guid>(
                LogLevel.Error,
                new EventId(3003, "ArtistDetailReindexError"),
                "Échec de la ré-indexation de l'artiste {ArtistId} après enrichissement à la demande.");

        private readonly IListeningStatsQueryPort _statsQueryPort = statsQueryPort;
        private readonly IGetOrEnrichArtistUseCase _getOrEnrichArtistUseCase = getOrEnrichArtistUseCase;
        private readonly ISearchEnginePort _searchEnginePort = searchEnginePort;
        private readonly ILogger<GetArtistDetailUseCase> _logger = logger;

        public async Task<ArtistDetail?> ExecuteAsync(GetArtistDetailQuery query, CancellationToken ct = default)
        {
            DomainArtist? artist = await _getOrEnrichArtistUseCase.ExecuteAsync(new GetOrEnrichArtistRequest(query.ArtistId), ct);

            if (artist is null)
            {
                return null;
            }

            var dateRange = new DateRange(query.From, query.To);
            ArtistDetail? detail = await _statsQueryPort.GetArtistDetailAsync(query.UserId, query.ArtistId, dateRange, ct);

            if (detail is not null)
            {
                // Ré-indexe systématiquement (cover potentiellement fraîche, ou index simplement pas
                // encore à jour depuis l'import). Coût négligeable : Meilisearch est local, contrairement
                // à l'appel Deezer ci-dessus qui, lui, ne se refait plus une fois l'artiste enrichi.
                await ReindexAsync(detail, ct);
            }

            return detail;
        }

        private async Task ReindexAsync(ArtistDetail detail, CancellationToken ct)
        {
            try
            {
                SearchDocumentDto document = SearchMapper.ToSearchDocument(detail.Id, detail.Name, detail.CoverUrl);
                await _searchEnginePort.IndexDocumentsAsync([document], ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logIndexingError(_logger, detail.Id, ex);
            }
        }
    }
}
