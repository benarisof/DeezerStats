using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Catalog;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.UseCases.Stats.Home
{
    /// <summary>
    /// Enrichit à la demande, en parallèle et à concurrence bornée, les éléments des trois listes
    /// (albums/artistes/morceaux) qui n'ont pas encore de couverture (voir
    /// CatalogEnrichmentCoordinator et GetTopAlbumsUseCase pour le raisonnement complet).
    /// </summary>
    public class GetHomeStatsUseCase(
        IListeningStatsQueryPort statsQueryPort,
        ICatalogEnrichmentCoordinator enrichmentCoordinator) : IGetHomeStatsUseCase
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = statsQueryPort;
        private readonly ICatalogEnrichmentCoordinator _enrichmentCoordinator = enrichmentCoordinator;

        public async Task<HomeStatsResponse> ExecuteAsync(GetHomeStatsQuery query, CancellationToken ct = default)
        {
            var dateRange = new DateRange(query.From, query.To);
            HomeStatsResponse result = await _statsQueryPort.GetHomeStatsAsync(query.UserId, dateRange, ct);

            IReadOnlyList<AlbumSummary> topAlbums = await CoverEnrichmentHelper.EnrichCoversAsync(result.TopAlbums, _enrichmentCoordinator, ct);
            IReadOnlyList<ArtistSummary> topArtists = await CoverEnrichmentHelper.EnrichCoversAsync(result.TopArtists, _enrichmentCoordinator, ct);
            IReadOnlyList<TrackSummary> topTracks = await CoverEnrichmentHelper.EnrichCoversAsync(result.TopTracks, _enrichmentCoordinator, ct);

            bool unchanged = ReferenceEquals(topAlbums, result.TopAlbums)
                && ReferenceEquals(topArtists, result.TopArtists)
                && ReferenceEquals(topTracks, result.TopTracks);

            return unchanged ? result : result with { TopAlbums = topAlbums, TopArtists = topArtists, TopTracks = topTracks };
        }
    }
}
