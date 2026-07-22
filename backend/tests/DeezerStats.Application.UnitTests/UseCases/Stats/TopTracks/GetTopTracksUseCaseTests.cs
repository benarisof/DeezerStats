using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Catalog;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Application.UseCases.Stats.TopTracks;
using DeezerStats.Application.Validation.Validators;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace DeezerStats.Application.UnitTests.UseCases.Stats.TopTracks
{
    public class GetTopTracksUseCaseTests
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = Substitute.For<IListeningStatsQueryPort>();
        private readonly ICatalogEnrichmentCoordinator _enrichmentCoordinator = Substitute.For<ICatalogEnrichmentCoordinator>();
        private readonly GetTopTracksUseCase _useCase;

        public GetTopTracksUseCaseTests()
        {
            _useCase = new GetTopTracksUseCase(_statsQueryPort, _enrichmentCoordinator, new GetTopTracksQueryValidator());
        }

        [Fact]
        public async Task ExecuteAsyncWithValidQueryShouldBuildDateRangeAndReturnPortResult()
        {
            // Arrange : liste vide -> aucun élément à enrichir, le résultat du port est retourné tel quel.
            var userId = Guid.NewGuid();
            var expected = new PagedResult<TrackSummary>([], 1, 20, 5);

            _statsQueryPort
                .GetTopTracksAsync(userId, Arg.Any<DateRange>(), 1, 20, Arg.Any<CancellationToken>())
                .Returns(expected);

            var query = new GetTopTracksQuery(userId, null, null, Page: 1, PageSize: 20);

            // Act
            PagedResult<TrackSummary> result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeSameAs(expected);
            await _enrichmentCoordinator.DidNotReceiveWithAnyArgs().EnrichTracksAsync(default!, default);
        }

        [Fact]
        public async Task ExecuteAsyncShouldEnrichOnlyTracksWithoutCoverAndPatchTheirFreshCover()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var alreadyCovered = new TrackSummary(Guid.NewGuid(), "One More Time", "Daft Punk", "Discovery", "https://existing.jpg", 10);
            var uncovered = new TrackSummary(Guid.NewGuid(), "Midnight City", "M83", "Hurry Up, We're Dreaming", null, 42);
            var expected = new PagedResult<TrackSummary>([alreadyCovered, uncovered], 1, 20, 2);

            _statsQueryPort
                .GetTopTracksAsync(userId, Arg.Any<DateRange>(), 1, 20, Arg.Any<CancellationToken>())
                .Returns(expected);

            _enrichmentCoordinator
                .EnrichTracksAsync(
                    Arg.Is<IReadOnlyCollection<Guid>>(ids => ids != null && ids.Count == 1 && ids.Contains(uncovered.Id)),
                    Arg.Any<CancellationToken>())
                .Returns(new Dictionary<Guid, string?> { [uncovered.Id] = "https://fresh.jpg" });

            var query = new GetTopTracksQuery(userId, null, null, Page: 1, PageSize: 20);

            // Act
            PagedResult<TrackSummary> result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Items.Should().ContainSingle(t => t.Id == alreadyCovered.Id && t.CoverUrl == "https://existing.jpg");
            result.Items.Should().ContainSingle(t => t.Id == uncovered.Id && t.CoverUrl == "https://fresh.jpg");
        }

        [Fact]
        public async Task ExecuteAsyncWithNegativePageShouldThrowValidationExceptionWithoutCallingPort()
        {
            // Arrange
            var query = new GetTopTracksQuery(Guid.NewGuid(), null, null, Page: -1, PageSize: 20);

            // Act
            Func<Task> act = () => _useCase.ExecuteAsync(query);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
            await _statsQueryPort.DidNotReceiveWithAnyArgs().GetTopTracksAsync(default, default!, default, default, default);
        }
    }
}
