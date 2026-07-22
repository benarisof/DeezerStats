using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.UseCases.Albums;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace DeezerStats.Application.UnitTests.UseCases
{
    public class GetOrEnrichAlbumUseCaseTests
    {
        private readonly IAlbumRepository _albumRepository = Substitute.For<IAlbumRepository>();
        private readonly IArtistRepository _artistRepository = Substitute.For<IArtistRepository>();
        private readonly IDeezerEnrichmentPort _deezerPort = Substitute.For<IDeezerEnrichmentPort>();
        private readonly GetOrEnrichAlbumUseCase _useCase;

        public GetOrEnrichAlbumUseCaseTests()
        {
            _useCase = new GetOrEnrichAlbumUseCase(_albumRepository, _artistRepository, _deezerPort);
        }

        [Fact]
        public async Task ExecuteAsyncWhenAlbumIsAlreadyEnrichedShouldReturnAlbumWithoutCallingDeezer()
        {
            // Arrange
            var album = new Album(Guid.NewGuid(), "Discovery", Guid.NewGuid());
            album.Enrich("https://cover.jpg", new DateOnly(2001, 3, 12), new Duration(3600));

            _albumRepository.GetByIdAsync(album.Id, Arg.Any<CancellationToken>()).Returns(album);

            var request = new GetOrEnrichAlbumRequest(album.Id);

            // Act
            Album? result = await _useCase.ExecuteAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.IsEnriched.Should().BeTrue();

            // Vérification : Deezer ne doit JAMAIS être appelé si la BDD est à jour
            await _deezerPort.DidNotReceiveWithAnyArgs().FetchAlbumMetadataAsync(default!, default!, default);
            await _albumRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
        }

        [Fact]
        public async Task ExecuteAsyncWhenAlbumIsNotEnrichedShouldFetchMetadataEnrichAlbumAndUpdateDb()
        {
            // Arrange
            var artist = new Artist(Guid.NewGuid(), "Daft Punk");
            var album = new Album(Guid.NewGuid(), "Discovery", artist.Id);
            var metadata = new DeezerAlbumMetadata("https://deezer.com/cover.jpg", new DateOnly(2001, 3, 12), new Duration(3600));

            _albumRepository.GetByIdAsync(album.Id, Arg.Any<CancellationToken>()).Returns(album);
            _artistRepository.GetByIdAsync(artist.Id, Arg.Any<CancellationToken>()).Returns(artist);
            _deezerPort.FetchAlbumMetadataAsync(album.Title, artist.Name, Arg.Any<CancellationToken>()).Returns(metadata);

            var request = new GetOrEnrichAlbumRequest(album.Id);

            // Act
            Album? result = await _useCase.ExecuteAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.IsEnriched.Should().BeTrue();
            result.CoverUrl.Should().Be("https://deezer.com/cover.jpg");
            result.ReleaseDate.Should().Be(new DateOnly(2001, 3, 12));
            result.Duration!.TotalSeconds.Should().Be(3600);

            // Vérification : la mise à jour en BDD a bien été ordonnée
            await _albumRepository.Received(1).UpdateAsync(album, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncWhenAlbumNotFoundInDbShouldReturnNull()
        {
            // Arrange
            var albumId = Guid.NewGuid();
            _albumRepository.GetByIdAsync(albumId, Arg.Any<CancellationToken>()).Returns((Album?)null);

            var request = new GetOrEnrichAlbumRequest(albumId);

            // Act
            Album? result = await _useCase.ExecuteAsync(request);

            // Assert
            result.Should().BeNull();
            await _deezerPort.DidNotReceiveWithAnyArgs().FetchAlbumMetadataAsync(default!, default!, default);
        }

        [Fact]
        public async Task ExecuteAsyncWhenArtistNotFoundShouldReturnAlbumUnenrichedWithoutCallingDeezer()
        {
            // Arrange : situation anormale (album orphelin), qui ne doit jamais faire échouer le
            // traitement des autres éléments enrichis en parallèle (voir
            // CatalogEnrichmentCoordinator).
            var album = new Album(Guid.NewGuid(), "Discovery", Guid.NewGuid());
            _albumRepository.GetByIdAsync(album.Id, Arg.Any<CancellationToken>()).Returns(album);
            _artistRepository.GetByIdAsync(album.ArtistId, Arg.Any<CancellationToken>()).Returns((Artist?)null);

            var request = new GetOrEnrichAlbumRequest(album.Id);

            // Act
            Album? result = await _useCase.ExecuteAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.IsEnriched.Should().BeFalse();
            await _deezerPort.DidNotReceiveWithAnyArgs().FetchAlbumMetadataAsync(default!, default!, default);
        }
    }
}
