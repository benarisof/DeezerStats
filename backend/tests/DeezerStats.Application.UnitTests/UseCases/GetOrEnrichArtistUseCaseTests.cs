using DeezerStats.Application.Ports;
using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.UseCases.Artists;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using FluentAssertions;
using NSubstitute;

namespace DeezerStats.Application.UnitTests.UseCases
{
    public class GetOrEnrichArtistUseCaseTests
    {
        private readonly IArtistRepository _artistRepository = Substitute.For<IArtistRepository>();
        private readonly IDeezerEnrichmentPort _deezerPort = Substitute.For<IDeezerEnrichmentPort>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly GetOrEnrichArtistUseCase _useCase;

        public GetOrEnrichArtistUseCaseTests()
        {
            _useCase = new GetOrEnrichArtistUseCase(_artistRepository, _deezerPort, _unitOfWork);
        }

        [Fact]
        public async Task ExecuteAsyncWhenArtistIsAlreadyEnrichedShouldReturnArtistWithoutCallingDeezer()
        {
            // Arrange
            var artist = new Artist(Guid.NewGuid(), "Daft Punk");
            artist.EnrichCover("https://cover.jpg");

            _artistRepository.GetByIdAsync(artist.Id, Arg.Any<CancellationToken>()).Returns(artist);

            var request = new GetOrEnrichArtistRequest(artist.Id);

            // Act
            Artist? result = await _useCase.ExecuteAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.IsEnriched.Should().BeTrue();

            // Vérification : Deezer ne doit JAMAIS être appelé si la BDD est à jour
            await _deezerPort.DidNotReceiveWithAnyArgs().FetchArtistMetadataAsync(default!, default);
            await _artistRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
            await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        }

        [Fact]
        public async Task ExecuteAsyncWhenArtistIsNotEnrichedShouldFetchMetadataEnrichArtistAndUpdateDb()
        {
            // Arrange
            var artist = new Artist(Guid.NewGuid(), "Daft Punk");
            var metadata = new DeezerArtistMetadata("https://deezer.com/artist-cover.jpg");

            _artistRepository.GetByIdAsync(artist.Id, Arg.Any<CancellationToken>()).Returns(artist);
            _deezerPort.FetchArtistMetadataAsync(artist.Name, Arg.Any<CancellationToken>()).Returns(metadata);

            var request = new GetOrEnrichArtistRequest(artist.Id);

            // Act
            Artist? result = await _useCase.ExecuteAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.IsEnriched.Should().BeTrue();
            result.CoverUrl.Should().Be("https://deezer.com/artist-cover.jpg");

            // Vérification : la mise à jour en BDD a bien été ordonnée, et réellement persistée
            // (UpdateAsync ne fait plus que suivre le changement, voir IArtistRepository.UpdateAsync).
            await _artistRepository.Received(1).UpdateAsync(artist, Arg.Any<CancellationToken>());
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncWhenDeezerHasNoPictureShouldReturnArtistUnenriched()
        {
            // Arrange : Deezer répond, mais sans photo (voir DeezerArtistMetadata.CoverUrl, nullable) —
            // ne doit pas faire échouer l'enrichissement, juste laisser l'artiste non enrichi.
            var artist = new Artist(Guid.NewGuid(), "Daft Punk");
            var metadata = new DeezerArtistMetadata(CoverUrl: null);

            _artistRepository.GetByIdAsync(artist.Id, Arg.Any<CancellationToken>()).Returns(artist);
            _deezerPort.FetchArtistMetadataAsync(artist.Name, Arg.Any<CancellationToken>()).Returns(metadata);

            var request = new GetOrEnrichArtistRequest(artist.Id);

            // Act
            Artist? result = await _useCase.ExecuteAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.IsEnriched.Should().BeFalse();

            // La mise à jour est tout de même appelée (comportement idempotent, cohérent avec
            // GetOrEnrichTrackUseCase/GetOrEnrichAlbumUseCase) : EnrichCover(null) est un no-op.
            await _artistRepository.Received(1).UpdateAsync(artist, Arg.Any<CancellationToken>());
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncWhenArtistNotFoundInDbShouldReturnNull()
        {
            // Arrange
            var artistId = Guid.NewGuid();
            _artistRepository.GetByIdAsync(artistId, Arg.Any<CancellationToken>()).Returns((Artist?)null);

            var request = new GetOrEnrichArtistRequest(artistId);

            // Act
            Artist? result = await _useCase.ExecuteAsync(request);

            // Assert
            result.Should().BeNull();
            await _deezerPort.DidNotReceiveWithAnyArgs().FetchArtistMetadataAsync(default!, default);
        }
    }
}
