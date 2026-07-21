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
    public class GetTopArtistsUseCaseTests
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = Substitute.For<IListeningStatsQueryPort>();
        private readonly GetTopArtistsUseCase _useCase;

        public GetTopArtistsUseCaseTests()
        {
            _useCase = new GetTopArtistsUseCase(_statsQueryPort, new GetTopArtistsQueryValidator());
        }

        [Fact]
        public async Task ExecuteAsyncWithValidQueryShouldBuildDateRangeAndReturnPortResult()
        {
            // Arrange
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
