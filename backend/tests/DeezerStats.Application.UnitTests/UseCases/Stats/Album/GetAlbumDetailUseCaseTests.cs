using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.ExternalServices.Search;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Application.UseCases.Albums;
using DeezerStats.Application.UseCases.Stats.Album;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using DomainAlbum = DeezerStats.Domain.Aggregates.AlbumAggregate.Album;

namespace DeezerStats.Application.UnitTests.UseCases.Stats.Album
{
    public class GetAlbumDetailUseCaseTests
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = Substitute.For<IListeningStatsQueryPort>();
        private readonly IGetOrEnrichAlbumUseCase _getOrEnrichAlbumUseCase = Substitute.For<IGetOrEnrichAlbumUseCase>();
        private readonly ISearchEnginePort _searchEnginePort = Substitute.For<ISearchEnginePort>();
        private readonly GetAlbumDetailUseCase _useCase;

        public GetAlbumDetailUseCaseTests()
        {
            _useCase = new GetAlbumDetailUseCase(
                _statsQueryPort,
                _getOrEnrichAlbumUseCase,
                _searchEnginePort,
                NullLogger<GetAlbumDetailUseCase>.Instance);
        }

        [Fact]
        public async Task ExecuteAsyncWhenAlbumExistsShouldEnrichThenBuildDateRangeAndReturnPortResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var albumId = Guid.NewGuid();
            var from = new DateOnly(2026, 1, 1);
            var album = new DomainAlbum(albumId, "Discovery", Guid.NewGuid());
            var expected = new AlbumDetail(albumId, "Discovery", album.ArtistId, "Daft Punk", null, null, null, 12.5, 42, []);

            _getOrEnrichAlbumUseCase
                .ExecuteAsync(Arg.Is<GetOrEnrichAlbumRequest>(r => r != null && r.AlbumId == albumId), Arg.Any<CancellationToken>())
                .Returns(album);

            _statsQueryPort
                .GetAlbumDetailAsync(userId, albumId, Arg.Is<DateRange>(r => r != null && r.From == from && r.To == null), Arg.Any<CancellationToken>())
                .Returns(expected);

            var query = new GetAlbumDetailQuery(userId, albumId, from, null);

            // Act
            AlbumDetail? result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeSameAs(expected);

            await _getOrEnrichAlbumUseCase.Received(1).ExecuteAsync(
                Arg.Is<GetOrEnrichAlbumRequest>(r => r != null && r.AlbumId == albumId),
                Arg.Any<CancellationToken>());

            // Vérification : le résultat frais (cover potentiellement mise à jour) est ré-indexé.
            await _searchEnginePort.Received(1).IndexDocumentsAsync(
                Arg.Is<IEnumerable<SearchDocumentDto>>(docs => docs != null
                    && docs.Single().Id == albumId.ToString()
                    && docs.Single().Label == "Discovery"
                    && docs.Single().Subtitle == "Daft Punk"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncWhenAlbumDoesNotExistShouldReturnNullWithoutQueryingStats()
        {
            // Arrange : le use case ne transforme pas le null en exception -- c'est au contrôleur de
            // le traduire en 404 (voir AlbumsController.GetAlbumDetail).
            var userId = Guid.NewGuid();
            var albumId = Guid.NewGuid();

            _getOrEnrichAlbumUseCase
                .ExecuteAsync(Arg.Any<GetOrEnrichAlbumRequest>(), Arg.Any<CancellationToken>())
                .Returns((DomainAlbum?)null);

            var query = new GetAlbumDetailQuery(userId, albumId, null, null);

            // Act
            AlbumDetail? result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeNull();

            await _statsQueryPort.DidNotReceiveWithAnyArgs().GetAlbumDetailAsync(default, default, default!, default);
            await _searchEnginePort.DidNotReceiveWithAnyArgs().IndexDocumentsAsync(default!, default);
        }

        [Fact]
        public async Task ExecuteAsyncShouldStillReturnResultWhenReindexingFails()
        {
            // Arrange : une panne du moteur de recherche ne doit jamais faire échouer la consultation
            // du détail -- la recherche est secondaire par rapport à l'affichage de la page.
            var userId = Guid.NewGuid();
            var albumId = Guid.NewGuid();
            var album = new DomainAlbum(albumId, "Discovery", Guid.NewGuid());
            var expected = new AlbumDetail(albumId, "Discovery", album.ArtistId, "Daft Punk", null, null, null, 12.5, 42, []);

            _getOrEnrichAlbumUseCase
                .ExecuteAsync(Arg.Any<GetOrEnrichAlbumRequest>(), Arg.Any<CancellationToken>())
                .Returns(album);

            _statsQueryPort
                .GetAlbumDetailAsync(userId, albumId, Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
                .Returns(expected);

            _searchEnginePort
                .IndexDocumentsAsync(Arg.Any<IEnumerable<SearchDocumentDto>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new InvalidOperationException("Meilisearch indisponible.")));

            var query = new GetAlbumDetailQuery(userId, albumId, null, null);

            // Act
            AlbumDetail? result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeSameAs(expected);
        }
    }
}
