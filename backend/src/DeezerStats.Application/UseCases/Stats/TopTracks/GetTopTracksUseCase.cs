using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Catalog;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Domain.ValueObjects;
using FluentValidation;

namespace DeezerStats.Application.UseCases.Stats.TopTracks
{
    /// <summary>
    /// Enrichit à la demande, en parallèle et à concurrence bornée, les morceaux de la page
    /// retournée qui n'ont pas encore de couverture (voir CatalogEnrichmentCoordinator et
    /// GetTopAlbumsUseCase pour le raisonnement complet).
    /// </summary>
    public class GetTopTracksUseCase(
        IListeningStatsQueryPort statsQueryPort,
        ICatalogEnrichmentCoordinator enrichmentCoordinator,
        IValidator<GetTopTracksQuery> validator) : IGetTopTracksUseCase
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = statsQueryPort;
        private readonly ICatalogEnrichmentCoordinator _enrichmentCoordinator = enrichmentCoordinator;
        private readonly IValidator<GetTopTracksQuery> _validator = validator;

        public async Task<PagedResult<TrackSummary>> ExecuteAsync(GetTopTracksQuery query, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(query, ct);

            var dateRange = new DateRange(query.From, query.To);
            PagedResult<TrackSummary> result = await _statsQueryPort.GetTopTracksAsync(query.UserId, dateRange, query.Page, query.PageSize, ct);

            IReadOnlyList<TrackSummary> enrichedItems = await CoverEnrichmentHelper.EnrichCoversAsync(result.Items, _enrichmentCoordinator, ct);

            return ReferenceEquals(enrichedItems, result.Items) ? result : result with { Items = enrichedItems };
        }
    }
}
