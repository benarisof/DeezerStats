using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Application.UseCases.Stats;
using DeezerStats.Application.Validation.Validators;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;
using FluentValidation;
using NSubstitute;

namespace DeezerStats.Application.UnitTests.UseCases.Stats
{
    public class GetTopAlbumsUseCaseTests
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = Substitute.For<IListeningStatsQueryPort>();
        private readonly GetTopAlbumsUseCase _useCase;

        public GetTopAlbumsUseCaseTests()
        {
            _useCase = new GetTopAlbumsUseCase(_statsQueryPort, new GetTopAlbumsQueryValidator());
        }

        [Fact]
        public async Task ExecuteAsyncWithValidQueryShouldBuildDateRangeAndReturnPortResult()
        {
            // Arrange
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
