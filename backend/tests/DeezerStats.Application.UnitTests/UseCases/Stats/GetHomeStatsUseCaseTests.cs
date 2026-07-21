using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Application.UseCases.Stats;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace DeezerStats.Application.UnitTests.UseCases.Stats
{
    public class GetHomeStatsUseCaseTests
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = Substitute.For<IListeningStatsQueryPort>();
        private readonly GetHomeStatsUseCase _useCase;

        public GetHomeStatsUseCaseTests()
        {
            _useCase = new GetHomeStatsUseCase(_statsQueryPort);
        }

        [Fact]
        public async Task ExecuteAsyncShouldBuildDateRangeFromQueryAndReturnPortResult()
        {
            // Arrange
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
    }
}
