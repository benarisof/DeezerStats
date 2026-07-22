using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.Ports.ExternalServices.Search;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.UseCases.Albums;
using DeezerStats.Application.UseCases.Artists;
using DeezerStats.Application.UseCases.Tracks;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.Adapters.Catalog;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DeezerStats.Infrastructure.UnitTests.Adapters.Catalog
{
    public class CatalogEnrichmentCoordinatorTests
    {
        [Fact]
        public async Task EnrichAlbumsAsyncShouldReturnFreshCoverAndReindexEachEnrichedAlbum()
        {
            // Arrange
            var artistId = Guid.NewGuid();
            var artist = new Artist(artistId, "Daft Punk");
            var album = new Album(Guid.NewGuid(), "Discovery", artistId);
            album.Enrich("https://fresh.jpg", new DateOnly(2001, 3, 7), new Duration(3600));

            IGetOrEnrichAlbumUseCase albumUseCase = Substitute.For<IGetOrEnrichAlbumUseCase>();
            albumUseCase.ExecuteAsync(Arg.Is<GetOrEnrichAlbumRequest>(r => r != null && r.AlbumId == album.Id), Arg.Any<CancellationToken>())
                .Returns(album);

            IArtistRepository artistRepository = Substitute.For<IArtistRepository>();
            artistRepository.GetByIdAsync(artistId, Arg.Any<CancellationToken>()).Returns(artist);

            ISearchEnginePort searchEnginePort = Substitute.For<ISearchEnginePort>();

            CatalogEnrichmentCoordinator coordinator = CreateCoordinator(
                albumUseCase: albumUseCase, artistRepository: artistRepository, searchEnginePort: searchEnginePort);

            // Act
            IReadOnlyDictionary<Guid, string?> result = await coordinator.EnrichAlbumsAsync([album.Id]);

            // Assert
            result.Should().ContainKey(album.Id).WhoseValue.Should().Be("https://fresh.jpg");

            await searchEnginePort.Received(1).IndexDocumentsAsync(
                Arg.Is<IEnumerable<SearchDocumentDto>>(docs => docs != null
                    && docs.Single().Id == album.Id.ToString()
                    && docs.Single().Subtitle == "Daft Punk"),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task EnrichArtistsAsyncShouldReturnFreshCoverAndReindex()
        {
            // Arrange
            var artist = new Artist(Guid.NewGuid(), "M83");
            artist.EnrichCover("https://fresh-artist.jpg");

            IGetOrEnrichArtistUseCase artistUseCase = Substitute.For<IGetOrEnrichArtistUseCase>();
            artistUseCase.ExecuteAsync(Arg.Is<GetOrEnrichArtistRequest>(r => r != null && r.ArtistId == artist.Id), Arg.Any<CancellationToken>())
                .Returns(artist);

            ISearchEnginePort searchEnginePort = Substitute.For<ISearchEnginePort>();

            CatalogEnrichmentCoordinator coordinator = CreateCoordinator(artistUseCase: artistUseCase, searchEnginePort: searchEnginePort);

            // Act
            IReadOnlyDictionary<Guid, string?> result = await coordinator.EnrichArtistsAsync([artist.Id]);

            // Assert
            result.Should().ContainKey(artist.Id).WhoseValue.Should().Be("https://fresh-artist.jpg");
            await searchEnginePort.Received(1).IndexDocumentsAsync(Arg.Any<IEnumerable<SearchDocumentDto>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task EnrichTracksAsyncShouldLookUpTrackByIdAndReindex()
        {
            // Arrange : CatalogEnrichmentCoordinator ne connaît que l'identifiant du morceau (voir
            // TrackSummary), pas son ISRC -- d'où l'usage de ExecuteByIdAsync plutôt qu'ExecuteAsync.
            var artistId = Guid.NewGuid();
            var artist = new Artist(artistId, "M83");
            var track = new Track(Guid.NewGuid(), new Isrc("USCM51300736"), "Midnight City", artistId, Guid.NewGuid());
            track.Enrich(new Duration(243), "https://fresh-track.jpg");

            IGetOrEnrichTrackUseCase trackUseCase = Substitute.For<IGetOrEnrichTrackUseCase>();
            trackUseCase.ExecuteByIdAsync(track.Id, Arg.Any<CancellationToken>()).Returns(track);

            IArtistRepository artistRepository = Substitute.For<IArtistRepository>();
            artistRepository.GetByIdAsync(artistId, Arg.Any<CancellationToken>()).Returns(artist);

            ISearchEnginePort searchEnginePort = Substitute.For<ISearchEnginePort>();

            CatalogEnrichmentCoordinator coordinator = CreateCoordinator(
                trackUseCase: trackUseCase, artistRepository: artistRepository, searchEnginePort: searchEnginePort);

            // Act
            IReadOnlyDictionary<Guid, string?> result = await coordinator.EnrichTracksAsync([track.Id]);

            // Assert
            result.Should().ContainKey(track.Id).WhoseValue.Should().Be("https://fresh-track.jpg");
            await searchEnginePort.Received(1).IndexDocumentsAsync(Arg.Any<IEnumerable<SearchDocumentDto>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task EnrichAlbumsAsyncWhenOneItemThrowsShouldStillReturnTheOthers()
        {
            // Arrange : un album fait échouer son enrichissement (ex. Deezer indisponible) -- ne
            // doit pas empêcher les autres albums d'être enrichis (concurrence bornée, voir
            // CatalogEnrichmentCoordinator).
            var artistId = Guid.NewGuid();
            var artist = new Artist(artistId, "Daft Punk");
            var failingAlbumId = Guid.NewGuid();
            var succeedingAlbum = new Album(Guid.NewGuid(), "Discovery", artistId);
            succeedingAlbum.Enrich("https://fresh.jpg", new DateOnly(2001, 3, 7), new Duration(3600));

            IGetOrEnrichAlbumUseCase albumUseCase = Substitute.For<IGetOrEnrichAlbumUseCase>();
            albumUseCase.ExecuteAsync(Arg.Is<GetOrEnrichAlbumRequest>(r => r != null && r.AlbumId == failingAlbumId), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<Album?>(new InvalidOperationException("Panne simulée.")));
            albumUseCase.ExecuteAsync(Arg.Is<GetOrEnrichAlbumRequest>(r => r != null && r.AlbumId == succeedingAlbum.Id), Arg.Any<CancellationToken>())
                .Returns(succeedingAlbum);

            IArtistRepository artistRepository = Substitute.For<IArtistRepository>();
            artistRepository.GetByIdAsync(artistId, Arg.Any<CancellationToken>()).Returns(artist);

            CatalogEnrichmentCoordinator coordinator = CreateCoordinator(albumUseCase: albumUseCase, artistRepository: artistRepository);

            // Act
            IReadOnlyDictionary<Guid, string?> result = await coordinator.EnrichAlbumsAsync([failingAlbumId, succeedingAlbum.Id]);

            // Assert
            result.Should().NotContainKey(failingAlbumId);
            result.Should().ContainKey(succeedingAlbum.Id).WhoseValue.Should().Be("https://fresh.jpg");
        }

        [Fact]
        public async Task EnrichAlbumsAsyncWithEmptyIdsShouldReturnEmptyResultWithoutCreatingAnyScope()
        {
            // Arrange
            CatalogEnrichmentCoordinator coordinator = CreateCoordinator();

            // Act
            IReadOnlyDictionary<Guid, string?> result = await coordinator.EnrichAlbumsAsync([]);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task EnrichAlbumsAsyncWhenReindexingFailsShouldStillReturnFreshCovers()
        {
            // Arrange : une panne du moteur de recherche ne doit jamais faire perdre le résultat de
            // l'enrichissement Deezer déjà persisté en base.
            var artistId = Guid.NewGuid();
            var artist = new Artist(artistId, "Daft Punk");
            var album = new Album(Guid.NewGuid(), "Discovery", artistId);
            album.Enrich("https://fresh.jpg", new DateOnly(2001, 3, 7), new Duration(3600));

            IGetOrEnrichAlbumUseCase albumUseCase = Substitute.For<IGetOrEnrichAlbumUseCase>();
            albumUseCase.ExecuteAsync(Arg.Any<GetOrEnrichAlbumRequest>(), Arg.Any<CancellationToken>()).Returns(album);

            IArtistRepository artistRepository = Substitute.For<IArtistRepository>();
            artistRepository.GetByIdAsync(artistId, Arg.Any<CancellationToken>()).Returns(artist);

            ISearchEnginePort searchEnginePort = Substitute.For<ISearchEnginePort>();
            searchEnginePort.IndexDocumentsAsync(Arg.Any<IEnumerable<SearchDocumentDto>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new InvalidOperationException("Meilisearch indisponible.")));

            CatalogEnrichmentCoordinator coordinator = CreateCoordinator(
                albumUseCase: albumUseCase, artistRepository: artistRepository, searchEnginePort: searchEnginePort);

            // Act
            IReadOnlyDictionary<Guid, string?> result = await coordinator.EnrichAlbumsAsync([album.Id]);

            // Assert
            result.Should().ContainKey(album.Id).WhoseValue.Should().Be("https://fresh.jpg");
        }

        private static CatalogEnrichmentCoordinator CreateCoordinator(
            IGetOrEnrichAlbumUseCase? albumUseCase = null,
            IGetOrEnrichArtistUseCase? artistUseCase = null,
            IGetOrEnrichTrackUseCase? trackUseCase = null,
            IArtistRepository? artistRepository = null,
            ISearchEnginePort? searchEnginePort = null)
        {
            var services = new ServiceCollection();
            services.AddScoped(_ => albumUseCase ?? Substitute.For<IGetOrEnrichAlbumUseCase>());
            services.AddScoped(_ => artistUseCase ?? Substitute.For<IGetOrEnrichArtistUseCase>());
            services.AddScoped(_ => trackUseCase ?? Substitute.For<IGetOrEnrichTrackUseCase>());
            services.AddScoped(_ => artistRepository ?? Substitute.For<IArtistRepository>());
            services.AddScoped(_ => searchEnginePort ?? Substitute.For<ISearchEnginePort>());
            ServiceProvider provider = services.BuildServiceProvider();

            return new CatalogEnrichmentCoordinator(
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<CatalogEnrichmentCoordinator>.Instance);
        }
    }
}
