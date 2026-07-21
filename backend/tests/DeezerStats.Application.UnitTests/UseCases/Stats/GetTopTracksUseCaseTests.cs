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
    public class GetTopTracksUseCaseTests
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = Substitute.For<IListeningStatsQueryPort>();
        private readonly GetTopTracksUseCase _useCase;

        public GetTopTracksUseCaseTests()
        {
            _useCase = new GetTopTracksUseCase(_statsQueryPort, new GetTopTracksQueryValidator());
        }

        [Fact]
        public async Task ExecuteAsyncWithValidQueryShouldBuildDateRangeAndReturnPortResult()
        {
            // Arrange
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
