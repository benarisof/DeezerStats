using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Catalog;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Domain.ValueObjects;
using FluentValidation;

namespace DeezerStats.Application.UseCases.Stats.TopAlbums
{
    /// <summary>
    /// Enrichit à la demande, en parallèle et à concurrence bornée, les albums de la page retournée
    /// qui n'ont pas encore de couverture (voir CatalogEnrichmentCoordinator) : contrairement aux
    /// pages de détail (un seul élément, voir GetAlbumDetailUseCase), cette liste peut compter
    /// jusqu'à 100 albums (voir StatsRules.MaxRankedResults), d'où la parallélisation nécessaire
    /// pour ne pas reproduire le blocage résolu côté import (voir ImportListeningHistoryUseCase).
    /// </summary>
    public class GetTopAlbumsUseCase(
        IListeningStatsQueryPort statsQueryPort,
        ICatalogEnrichmentCoordinator enrichmentCoordinator,
        IValidator<GetTopAlbumsQuery> validator) : IGetTopAlbumsUseCase
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = statsQueryPort;
        private readonly ICatalogEnrichmentCoordinator _enrichmentCoordinator = enrichmentCoordinator;
        private readonly IValidator<GetTopAlbumsQuery> _validator = validator;

        public async Task<PagedResult<AlbumSummary>> ExecuteAsync(GetTopAlbumsQuery query, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(query, ct);

            var dateRange = new DateRange(query.From, query.To);
            PagedResult<AlbumSummary> result = await _statsQueryPort.GetTopAlbumsAsync(query.UserId, dateRange, query.Page, query.PageSize, ct);

            IReadOnlyList<AlbumSummary> enrichedItems = await CoverEnrichmentHelper.EnrichCoversAsync(result.Items, _enrichmentCoordinator, ct);

            return ReferenceEquals(enrichedItems, result.Items) ? result : result with { Items = enrichedItems };
        }
    }
}
