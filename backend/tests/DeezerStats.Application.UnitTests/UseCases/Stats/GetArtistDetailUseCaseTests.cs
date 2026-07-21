using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Application.UseCases.Stats;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace DeezerStats.Application.UnitTests.UseCases.Stats
{
    public class GetArtistDetailUseCaseTests
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = Substitute.For<IListeningStatsQueryPort>();
        private readonly GetArtistDetailUseCase _useCase;

        public GetArtistDetailUseCaseTests()
        {
            _useCase = new GetArtistDetailUseCase(_statsQueryPort);
        }

        [Fact]
        public async Task ExecuteAsyncWhenArtistExistsShouldBuildDateRangeAndReturnPortResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var artistId = Guid.NewGuid();
            var expected = new ArtistDetail(artistId, "Daft Punk", null, 2, 10, 5.5, 30, []);

            _statsQueryPort
                .GetArtistDetailAsync(userId, artistId, Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
                .Returns(expected);

            var query = new GetArtistDetailQuery(userId, artistId, null, null);

            // Act
            ArtistDetail? result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task ExecuteAsyncWhenArtistDoesNotExistShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var artistId = Guid.NewGuid();

            _statsQueryPort
                .GetArtistDetailAsync(userId, artistId, Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
                .Returns((ArtistDetail?)null);

            var query = new GetArtistDetailQuery(userId, artistId, null, null);

            // Act
            ArtistDetail? result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeNull();
        }
    }
}
