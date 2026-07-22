using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.ExternalServices.Search;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Application.UseCases.Artists;
using DeezerStats.Application.UseCases.Stats.Artist;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using DomainArtist = DeezerStats.Domain.Aggregates.ArtistAggregate.Artist;

namespace DeezerStats.Application.UnitTests.UseCases.Stats.Artist
{
    public class GetArtistDetailUseCaseTests
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = Substitute.For<IListeningStatsQueryPort>();
        private readonly IGetOrEnrichArtistUseCase _getOrEnrichArtistUseCase = Substitute.For<IGetOrEnrichArtistUseCase>();
        private readonly ISearchEnginePort _searchEnginePort = Substitute.For<ISearchEnginePort>();
        private readonly GetArtistDetailUseCase _useCase;

        public GetArtistDetailUseCaseTests()
        {
            _useCase = new GetArtistDetailUseCase(
                _statsQueryPort,
                _getOrEnrichArtistUseCase,
                _searchEnginePort,
                NullLogger<GetArtistDetailUseCase>.Instance);
        }

        [Fact]
        public async Task ExecuteAsyncWhenArtistExistsShouldEnrichThenBuildDateRangeAndReturnPortResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var artistId = Guid.NewGuid();
            var artist = new DomainArtist(artistId, "Daft Punk");
            var expected = new ArtistDetail(artistId, "Daft Punk", null, 2, 10, 5.5, 30, []);

            _getOrEnrichArtistUseCase
                .ExecuteAsync(Arg.Is<GetOrEnrichArtistRequest>(r => r != null && r.ArtistId == artistId), Arg.Any<CancellationToken>())
                .Returns(artist);

            _statsQueryPort
                .GetArtistDetailAsync(userId, artistId, Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
                .Returns(expected);

            var query = new GetArtistDetailQuery(userId, artistId, null, null);

            // Act
            ArtistDetail? result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeSameAs(expected);

            await _getOrEnrichArtistUseCase.Received(1).ExecuteAsync(
                Arg.Is<GetOrEnrichArtistRequest>(r => r != null && r.ArtistId == artistId),
                Arg.Any<CancellationToken>());

            await _searchEnginePort.Received(1).IndexDocumentsAsync(
                Arg.Is<IEnumerable<SearchDocumentDto>>(docs => docs != null
                    && docs.Single().Id == artistId.ToString()
                    && docs.Single().Label == "Daft Punk"
                    && docs.Single().Type == "artist"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncWhenArtistDoesNotExistShouldReturnNullWithoutQueryingStats()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var artistId = Guid.NewGuid();

            _getOrEnrichArtistUseCase
                .ExecuteAsync(Arg.Any<GetOrEnrichArtistRequest>(), Arg.Any<CancellationToken>())
                .Returns((DomainArtist?)null);

            var query = new GetArtistDetailQuery(userId, artistId, null, null);

            // Act
            ArtistDetail? result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeNull();

            await _statsQueryPort.DidNotReceiveWithAnyArgs().GetArtistDetailAsync(default, default, default!, default);
            await _searchEnginePort.DidNotReceiveWithAnyArgs().IndexDocumentsAsync(default!, default);
        }

        [Fact]
        public async Task ExecuteAsyncShouldStillReturnResultWhenReindexingFails()
        {
            // Arrange : une panne du moteur de recherche ne doit jamais faire échouer la consultation
            // du détail -- la recherche est secondaire par rapport à l'affichage de la page.
            var userId = Guid.NewGuid();
            var artistId = Guid.NewGuid();
            var artist = new DomainArtist(artistId, "Daft Punk");
            var expected = new ArtistDetail(artistId, "Daft Punk", null, 2, 10, 5.5, 30, []);

            _getOrEnrichArtistUseCase
                .ExecuteAsync(Arg.Any<GetOrEnrichArtistRequest>(), Arg.Any<CancellationToken>())
                .Returns(artist);

            _statsQueryPort
                .GetArtistDetailAsync(userId, artistId, Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
                .Returns(expected);

            _searchEnginePort
                .IndexDocumentsAsync(Arg.Any<IEnumerable<SearchDocumentDto>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new InvalidOperationException("Meilisearch indisponible.")));

            var query = new GetArtistDetailQuery(userId, artistId, null, null);

            // Act
            ArtistDetail? result = await _useCase.ExecuteAsync(query);

            // Assert
            result.Should().BeSameAs(expected);
        }
    }
}
