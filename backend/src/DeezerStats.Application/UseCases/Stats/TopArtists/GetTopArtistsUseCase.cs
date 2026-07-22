using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Catalog;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Domain.ValueObjects;
using FluentValidation;

namespace DeezerStats.Application.UseCases.Stats.TopArtists
{
    /// <summary>
    /// Enrichit à la demande, en parallèle et à concurrence bornée, les artistes de la page
    /// retournée qui n'ont pas encore de couverture (voir CatalogEnrichmentCoordinator et
    /// GetTopAlbumsUseCase pour le raisonnement complet).
    /// </summary>
    public class GetTopArtistsUseCase(
        IListeningStatsQueryPort statsQueryPort,
        ICatalogEnrichmentCoordinator enrichmentCoordinator,
        IValidator<GetTopArtistsQuery> validator) : IGetTopArtistsUseCase
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = statsQueryPort;
        private readonly ICatalogEnrichmentCoordinator _enrichmentCoordinator = enrichmentCoordinator;
        private readonly IValidator<GetTopArtistsQuery> _validator = validator;

        public async Task<PagedResult<ArtistSummary>> ExecuteAsync(GetTopArtistsQuery query, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(query, ct);

            var dateRange = new DateRange(query.From, query.To);
            PagedResult<ArtistSummary> result = await _statsQueryPort.GetTopArtistsAsync(query.UserId, dateRange, query.Page, query.PageSize, ct);

            IReadOnlyList<ArtistSummary> enrichedItems = await CoverEnrichmentHelper.EnrichCoversAsync(result.Items, _enrichmentCoordinator, ct);

            return ReferenceEquals(enrichedItems, result.Items) ? result : result with { Items = enrichedItems };
        }
    }
}
