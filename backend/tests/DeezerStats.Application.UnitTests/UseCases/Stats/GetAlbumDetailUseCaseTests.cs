using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Application.UseCases.Stats;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace DeezerStats.Application.UnitTests.UseCases.Stats
{
    public class GetAlbumDetailUseCaseTests
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = Substitute.For<IListeningStatsQueryPort>();
        private readonly GetAlbumDetailUseCase _useCase;

        public GetAlbumDetailUseCaseTests()
        {
            _useCase = new GetAlbumDetailUseCase(_statsQueryPort);
        }

        [Fact]
        public async Task ExecuteAsyncWhenAlbumExistsShouldBuildDateRangeAndReturnPortResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var albumId = Guid.NewGuid();
            var from = new DateOnly(2026, 1, 1);
            var expected = new AlbumDetail(albumId, "Discovery", Guid.NewGuid(), "Daft Punk", null, null, null, 12.5, 42, []);

            _statsQueryPort
                .GetAlbumDetailAsync(userId, albumId, Arg.Is<DateRange>(r => r != null && r.From == from && r.To == null), Arg.Any<CancellationToken>())
                .Returns(expected);

            var query = new GetAlbumDetailQuery(userId, albumId, from, null);

            // Act
            AlbumDetail? result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task ExecuteAsyncWhenAlbumDoesNotExistShouldReturnNull()
        {
            // Arrange : le use case ne transforme pas le null en exception -- c'est au contrôleur de
            // le traduire en 404 (voir AlbumsController.GetAlbumDetail).
            var userId = Guid.NewGuid();
            var albumId = Guid.NewGuid();

            _statsQueryPort
                .GetAlbumDetailAsync(userId, albumId, Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
                .Returns((AlbumDetail?)null);

            var query = new GetAlbumDetailQuery(userId, albumId, null, null);

            // Act
            AlbumDetail? result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeNull();
        }
    }
}
