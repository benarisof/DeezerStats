using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Catalog;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Application.UseCases.Stats.Home;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace DeezerStats.Application.UnitTests.UseCases.Stats.Home
{
    public class GetHomeStatsUseCaseTests
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = Substitute.For<IListeningStatsQueryPort>();
        private readonly ICatalogEnrichmentCoordinator _enrichmentCoordinator = Substitute.For<ICatalogEnrichmentCoordinator>();
        private readonly GetHomeStatsUseCase _useCase;

        public GetHomeStatsUseCaseTests()
        {
            _useCase = new GetHomeStatsUseCase(_statsQueryPort, _enrichmentCoordinator);
        }

        [Fact]
        public async Task ExecuteAsyncShouldBuildDateRangeFromQueryAndReturnPortResult()
        {
            // Arrange : listes vides -> aucun élément à enrichir, le résultat du port est retourné tel quel.
            var userId = Guid.NewGuid();
            var from = new DateOnly(2026, 1, 1);
            var to = new DateOnly(2026, 6, 30);
            var expected = new HomeStatsResponse([], [], []);

            _statsQueryPort
                .GetHomeStatsAsync(userId, Arg.Is<DateRange>(r => r != null && r.From == from && r.To == to), Arg.Any<CancellationToken>())
                .Returns(expected);

            var query = new GetHomeStatsQuery(userId, from, to);

            // Act
            HomeStatsResponse result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task ExecuteAsyncWithoutDateBoundsShouldPassAnUnboundedDateRange()
        {
            // Arrange : from/to absents -> DateRange(null, null), pas de filtrage de date.
            var userId = Guid.NewGuid();
            var expected = new HomeStatsResponse([], [], []);

            _statsQueryPort
                .GetHomeStatsAsync(userId, Arg.Is<DateRange>(r => r != null && r.From == null && r.To == null), Arg.Any<CancellationToken>())
                .Returns(expected);

            var query = new GetHomeStatsQuery(userId, null, null);

            // Act
            HomeStatsResponse result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task ExecuteAsyncShouldEnrichUncoveredItemsAcrossAllThreeLists()
        {
            // Arrange : un élément sans couverture par liste (voir CatalogEnrichmentCoordinator).
            var userId = Guid.NewGuid();
            var album = new AlbumSummary(Guid.NewGuid(), "Discovery", "Daft Punk", null, 42);
            var artist = new ArtistSummary(Guid.NewGuid(), "M83", null, 30);
            var track = new TrackSummary(Guid.NewGuid(), "Midnight City", "M83", "Hurry Up, We're Dreaming", null, 15);
            var expected = new HomeStatsResponse([album], [artist], [track]);

            _statsQueryPort
                .GetHomeStatsAsync(userId, Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
                .Returns(expected);

            _enrichmentCoordinator
                .EnrichAlbumsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
                .Returns(new Dictionary<Guid, string?> { [album.Id] = "https://fresh-album.jpg" });
            _enrichmentCoordinator
                .EnrichArtistsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
                .Returns(new Dictionary<Guid, string?> { [artist.Id] = "https://fresh-artist.jpg" });
            _enrichmentCoordinator
                .EnrichTracksAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
                .Returns(new Dictionary<Guid, string?> { [track.Id] = "https://fresh-track.jpg" });

            var query = new GetHomeStatsQuery(userId, null, null);

            // Act
            HomeStatsResponse result = await _useCase.ExecuteAsync(query);

            // Assert
            result.TopAlbums.Should().ContainSingle(a => a.CoverUrl == "https://fresh-album.jpg");
            result.TopArtists.Should().ContainSingle(a => a.CoverUrl == "https://fresh-artist.jpg");
            result.TopTracks.Should().ContainSingle(t => t.CoverUrl == "https://fresh-track.jpg");
        }
    }
}
