using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.UseCases.Tracks;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace DeezerStats.Application.UnitTests.UseCases
{
    public class GetOrEnrichTrackUseCaseTests
    {
        private readonly ITrackRepository _trackRepository = Substitute.For<ITrackRepository>();
        private readonly IDeezerEnrichmentPort _deezerPort = Substitute.For<IDeezerEnrichmentPort>();
        private readonly GetOrEnrichTrackUseCase _useCase;

        public GetOrEnrichTrackUseCaseTests()
        {
            _useCase = new GetOrEnrichTrackUseCase(_trackRepository, _deezerPort);
        }

        [Fact]
        public async Task ExecuteAsyncWhenTrackIsAlreadyEnrichedShouldReturnTrackWithoutCallingDeezer()
        {
            // Arrange
            var isrc = new Isrc("USCM51300736");
            var track = new Track(Guid.NewGuid(), isrc, "Midnight City", Guid.NewGuid(), Guid.NewGuid());
            track.Enrich(new Duration(240), "https://cover.jpg");

            _trackRepository.GetByIsrcAsync(isrc, Arg.Any<CancellationToken>())
                .Returns(track);

            var request = new GetOrEnrichTrackRequest(isrc);

            // Act
            Track? result = await _useCase.ExecuteAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.IsEnriched.Should().BeTrue();

            // Vérification : Deezer ne doit JAMAIS être appelé si la BDD est à jour
            await _deezerPort.DidNotReceiveWithAnyArgs()
                .FetchTrackMetadataAsync(default!, default);

            await _trackRepository.DidNotReceiveWithAnyArgs()
                .UpdateAsync(default!, default);
        }

        [Fact]
        public async Task ExecuteAsyncWhenTrackIsNotEnrichedShouldFetchMetadataEnrichTrackAndUpdateDb()
        {
            // Arrange
            var isrc = new Isrc("USCM51300736");
            var track = new Track(Guid.NewGuid(), isrc, "Midnight City", Guid.NewGuid(), Guid.NewGuid());
            var metadata = new DeezerTrackMetadata("https://deezer.com/cover.jpg", new Duration(243));

            _trackRepository.GetByIsrcAsync(isrc, Arg.Any<CancellationToken>())
                .Returns(track);

            _deezerPort.FetchTrackMetadataAsync(isrc, Arg.Any<CancellationToken>())
                .Returns(metadata);

            var request = new GetOrEnrichTrackRequest(isrc);

            // Act
            Track? result = await _useCase.ExecuteAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.IsEnriched.Should().BeTrue();
            result.Duration!.TotalSeconds.Should().Be(243);
            result.CoverUrl.Should().Be("https://deezer.com/cover.jpg");

            // Vérification : La mise à jour en BDD a bien été ordonnée
            await _trackRepository.Received(1).UpdateAsync(track, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncWhenTrackNotFoundInDbShouldReturnNull()
        {
            // Arrange
            var isrc = new Isrc("USCM51300736");
            _trackRepository.GetByIsrcAsync(isrc, Arg.Any<CancellationToken>())
                .Returns((Track?)null);

            var request = new GetOrEnrichTrackRequest(isrc);

            // Act
            Track? result = await _useCase.ExecuteAsync(request);

            // Assert
            result.Should().BeNull();
            await _deezerPort.DidNotReceiveWithAnyArgs().FetchTrackMetadataAsync(default!, default);
        }

        [Fact]
        public async Task ExecuteByIdAsyncWhenTrackIsNotEnrichedShouldLookUpByIdFetchMetadataAndUpdateDb()
        {
            // Arrange : utilisé par CatalogEnrichmentCoordinator, qui ne connaît que l'identifiant du
            // morceau (issu des DTO de liste, voir TrackSummary) et non son ISRC.
            var isrc = new Isrc("USCM51300736");
            var trackId = Guid.NewGuid();
            var track = new Track(trackId, isrc, "Midnight City", Guid.NewGuid(), Guid.NewGuid());
            var metadata = new DeezerTrackMetadata("https://deezer.com/cover.jpg", new Duration(243));

            _trackRepository.GetByIdAsync(trackId, Arg.Any<CancellationToken>())
                .Returns(track);

            _deezerPort.FetchTrackMetadataAsync(isrc, Arg.Any<CancellationToken>())
                .Returns(metadata);

            // Act
            Track? result = await _useCase.ExecuteByIdAsync(trackId);

            // Assert
            result.Should().NotBeNull();
            result!.CoverUrl.Should().Be("https://deezer.com/cover.jpg");
            await _trackRepository.Received(1).UpdateAsync(track, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteByIdAsyncWhenTrackNotFoundInDbShouldReturnNull()
        {
            // Arrange
            var trackId = Guid.NewGuid();
            _trackRepository.GetByIdAsync(trackId, Arg.Any<CancellationToken>())
                .Returns((Track?)null);

            // Act
            Track? result = await _useCase.ExecuteByIdAsync(trackId);

            // Assert
            result.Should().BeNull();
            await _deezerPort.DidNotReceiveWithAnyArgs().FetchTrackMetadataAsync(default!, default);
        }
    }
}
