using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Catalog;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Application.UseCases.Stats.TopArtists;
using DeezerStats.Application.Validation.Validators;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace DeezerStats.Application.UnitTests.UseCases.Stats.TopArtists
{
    public class GetTopArtistsUseCaseTests
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = Substitute.For<IListeningStatsQueryPort>();
        private readonly ICatalogEnrichmentCoordinator _enrichmentCoordinator = Substitute.For<ICatalogEnrichmentCoordinator>();
        private readonly GetTopArtistsUseCase _useCase;

        public GetTopArtistsUseCaseTests()
        {
            _useCase = new GetTopArtistsUseCase(_statsQueryPort, _enrichmentCoordinator, new GetTopArtistsQueryValidator());
        }

        [Fact]
        public async Task ExecuteAsyncWithValidQueryShouldBuildDateRangeAndReturnPortResult()
        {
            // Arrange : liste vide -> aucun élément à enrichir, le résultat du port est retourné tel quel.
            var userId = Guid.NewGuid();
            var expected = new PagedResult<ArtistSummary>([], 1, 20, 5);

            _statsQueryPort
                .GetTopArtistsAsync(userId, Arg.Any<DateRange>(), 1, 20, Arg.Any<CancellationToken>())
                .Returns(expected);

            var query = new GetTopArtistsQuery(userId, null, null, Page: 1, PageSize: 20);

            // Act
            PagedResult<ArtistSummary> result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeSameAs(expected);
            await _enrichmentCoordinator.DidNotReceiveWithAnyArgs().EnrichArtistsAsync(default!, default);
        }

        [Fact]
        public async Task ExecuteAsyncShouldEnrichOnlyArtistsWithoutCoverAndPatchTheirFreshCover()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var alreadyCovered = new ArtistSummary(Guid.NewGuid(), "Daft Punk", "https://existing.jpg", 10);
            var uncovered = new ArtistSummary(Guid.NewGuid(), "M83", null, 42);
            var expected = new PagedResult<ArtistSummary>([alreadyCovered, uncovered], 1, 20, 2);

            _statsQueryPort
                .GetTopArtistsAsync(userId, Arg.Any<DateRange>(), 1, 20, Arg.Any<CancellationToken>())
                .Returns(expected);

            _enrichmentCoordinator
                .EnrichArtistsAsync(
                    Arg.Is<IReadOnlyCollection<Guid>>(ids => ids != null && ids.Count == 1 && ids.Contains(uncovered.Id)),
                    Arg.Any<CancellationToken>())
                .Returns(new Dictionary<Guid, string?> { [uncovered.Id] = "https://fresh.jpg" });

            var query = new GetTopArtistsQuery(userId, null, null, Page: 1, PageSize: 20);

            // Act
            PagedResult<ArtistSummary> result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Items.Should().ContainSingle(a => a.Id == alreadyCovered.Id && a.CoverUrl == "https://existing.jpg");
            result.Items.Should().ContainSingle(a => a.Id == uncovered.Id && a.CoverUrl == "https://fresh.jpg");
        }

        [Fact]
        public async Task ExecuteAsyncWithInvalidPageSizeShouldThrowValidationExceptionWithoutCallingPort()
        {
            // Arrange
            var query = new GetTopArtistsQuery(Guid.NewGuid(), null, null, Page: 1, PageSize: 0);

            // Act
            Func<Task> act = () => _useCase.ExecuteAsync(query);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
            await _statsQueryPort.DidNotReceiveWithAnyArgs().GetTopArtistsAsync(default, default!, default, default, default);
        }
    }
}
