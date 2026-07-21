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
    public class GetHistoryUseCaseTests
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = Substitute.For<IListeningStatsQueryPort>();
        private readonly GetHistoryUseCase _useCase;

        public GetHistoryUseCaseTests()
        {
            _useCase = new GetHistoryUseCase(_statsQueryPort, new GetHistoryQueryValidator());
        }

        [Fact]
        public async Task ExecuteAsyncWithValidQueryShouldBuildDateRangeAndReturnPortResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expected = new PagedResult<HistoryEntry>([], 1, 20, 5);

            _statsQueryPort
                .GetHistoryAsync(userId, Arg.Any<DateRange>(), 1, 20, Arg.Any<CancellationToken>())
                .Returns(expected);

            var query = new GetHistoryQuery(userId, null, null, Page: 1, PageSize: 20);

            // Act
            PagedResult<HistoryEntry> result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task ExecuteAsyncWithPageSizeAboveMaxRankedResultsShouldThrowValidationExceptionWithoutCallingPort()
        {
            // Arrange
            var query = new GetHistoryQuery(Guid.NewGuid(), null, null, Page: 1, PageSize: 500);

            // Act
            Func<Task> act = () => _useCase.ExecuteAsync(query);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
            await _statsQueryPort.DidNotReceiveWithAnyArgs().GetHistoryAsync(default, default!, default, default, default);
        }
    }
}
