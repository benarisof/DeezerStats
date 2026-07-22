using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Catalog;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Application.UseCases.Stats.TopAlbums;
using DeezerStats.Application.Validation.Validators;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace DeezerStats.Application.UnitTests.UseCases.Stats.TopAlbums
{
    public class GetTopAlbumsUseCaseTests
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = Substitute.For<IListeningStatsQueryPort>();
        private readonly ICatalogEnrichmentCoordinator _enrichmentCoordinator = Substitute.For<ICatalogEnrichmentCoordinator>();
        private readonly GetTopAlbumsUseCase _useCase;

        public GetTopAlbumsUseCaseTests()
        {
            _useCase = new GetTopAlbumsUseCase(_statsQueryPort, _enrichmentCoordinator, new GetTopAlbumsQueryValidator());
        }

        [Fact]
        public async Task ExecuteAsyncWithValidQueryShouldBuildDateRangeAndReturnPortResult()
        {
            // Arrange : liste vide -> aucun élément à enrichir, le résultat du port est retourné tel quel.
            var userId = Guid.NewGuid();
            var from = new DateOnly(2026, 1, 1);
            var to = new DateOnly(2026, 6, 30);
            var expected = new PagedResult<AlbumSummary>([], 2, 10, 25);

            _statsQueryPort
                .GetTopAlbumsAsync(userId, Arg.Is<DateRange>(r => r != null && r.From == from && r.To == to), 2, 10, Arg.Any<CancellationToken>())
                .Returns(expected);

            var query = new GetTopAlbumsQuery(userId, from, to, Page: 2, PageSize: 10);

            // Act
            PagedResult<AlbumSummary> result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeSameAs(expected);
            await _enrichmentCoordinator.DidNotReceiveWithAnyArgs().EnrichAlbumsAsync(default!, default);
        }

        [Fact]
        public async Task ExecuteAsyncShouldEnrichOnlyAlbumsWithoutCoverAndPatchTheirFreshCover()
        {
            // Arrange : un album déjà couvert (jamais enrichi à nouveau) et un album sans couverture
            // (à enrichir à la demande, voir CatalogEnrichmentCoordinator).
            var userId = Guid.NewGuid();
            var alreadyCovered = new AlbumSummary(Guid.NewGuid(), "Random Access Memories", "Daft Punk", "https://existing.jpg", 10);
            var uncovered = new AlbumSummary(Guid.NewGuid(), "Discovery", "Daft Punk", null, 42);
            var expected = new PagedResult<AlbumSummary>([alreadyCovered, uncovered], 1, 20, 2);

            _statsQueryPort
                .GetTopAlbumsAsync(userId, Arg.Any<DateRange>(), 1, 20, Arg.Any<CancellationToken>())
                .Returns(expected);

            _enrichmentCoordinator
                .EnrichAlbumsAsync(
                    Arg.Is<IReadOnlyCollection<Guid>>(ids => ids != null && ids.Count == 1 && ids.Contains(uncovered.Id)),
                    Arg.Any<CancellationToken>())
                .Returns(new Dictionary<Guid, string?> { [uncovered.Id] = "https://fresh.jpg" });

            var query = new GetTopAlbumsQuery(userId, null, null, Page: 1, PageSize: 20);

            // Act
            PagedResult<AlbumSummary> result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Items.Should().ContainSingle(a => a.Id == alreadyCovered.Id && a.CoverUrl == "https://existing.jpg");
            result.Items.Should().ContainSingle(a => a.Id == uncovered.Id && a.CoverUrl == "https://fresh.jpg");
        }

        [Fact]
        public async Task ExecuteAsyncWithPageBelowOneShouldThrowValidationExceptionWithoutCallingPort()
        {
            // Arrange
            var query = new GetTopAlbumsQuery(Guid.NewGuid(), null, null, Page: 0, PageSize: 10);

            // Act
            Func<Task> act = () => _useCase.ExecuteAsync(query);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
            await _statsQueryPort.DidNotReceiveWithAnyArgs().GetTopAlbumsAsync(default, default!, default, default, default);
        }

        [Fact]
        public async Task ExecuteAsyncWithPageSizeAboveMaxRankedResultsShouldThrowValidationException()
        {
            // Arrange : StatsRules.MaxRankedResults = 100.
            var query = new GetTopAlbumsQuery(Guid.NewGuid(), null, null, Page: 1, PageSize: 101);

            // Act
            Func<Task> act = () => _useCase.ExecuteAsync(query);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }
    }
}
