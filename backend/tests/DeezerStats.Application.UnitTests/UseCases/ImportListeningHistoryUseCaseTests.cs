using DeezerStats.Application.DTOs;
using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.Ports;
using DeezerStats.Application.Ports.ExternalServices.Excel;
using DeezerStats.Application.Ports.ExternalServices.Search;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.UseCases.Import;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.ListeningEventAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DeezerStats.Application.UnitTests.UseCases
{
    public class ImportListeningHistoryUseCaseTests
    {
        private readonly IExcelParserPort _excelParser = Substitute.For<IExcelParserPort>();
        private readonly IListeningEventRepository _listeningRepository = Substitute.For<IListeningEventRepository>();
        private readonly ITrackRepository _trackRepository = Substitute.For<ITrackRepository>();
        private readonly IArtistRepository _artistRepository = Substitute.For<IArtistRepository>();
        private readonly IAlbumRepository _albumRepository = Substitute.For<IAlbumRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly ISearchEnginePort _searchEnginePort = Substitute.For<ISearchEnginePort>();

        private readonly ImportListeningHistoryUseCase _useCase;

        public ImportListeningHistoryUseCaseTests()
        {
            _useCase = new ImportListeningHistoryUseCase(
                _excelParser,
                _listeningRepository,
                _trackRepository,
                _artistRepository,
                _albumRepository,
                _unitOfWork,
                _searchEnginePort,
                NullLogger<ImportListeningHistoryUseCase>.Instance);

            _trackRepository.GetByIsrcsAsync(Arg.Any<IEnumerable<Isrc>>(), Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<Track>)[]);
            _listeningRepository.GetExistingListenedAtsAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
                .Returns(new Dictionary<Guid, HashSet<DateTime>>());
            _artistRepository.GetByNamesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<Artist>)[]);
            _albumRepository.GetByArtistIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<Album>)[]);
        }

        [Fact]
        public async Task ExecuteAsyncWithValidRowsShouldImportSuccessfullyAndCreateCatalogEntities()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var stream = new MemoryStream();
            DateTime listenedAt = DateTime.UtcNow.AddHours(-1);

            var row = new ExcelListeningRow(
                TrackTitle: "Starboy",
                ArtistName: "The Weeknd",
                AlbumTitle: "Starboy Album",
                Isrc: "USUM71607007",
                DurationInSeconds: 230,
                ListenedAt: listenedAt);

            _excelParser.ParseHistoryAsync(stream, Arg.Any<CancellationToken>())
                .Returns([row]);

            var command = new ImportListeningHistoryCommand(userId, stream);

            // Act
            ImportReport result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(1);
            result.SkippedCount.Should().Be(0);
            result.ErrorCount.Should().Be(0);

            await _artistRepository.Received(1).AddRangeAsync(
                Arg.Is<IEnumerable<Artist>>(a => a != null && a.Count() == 1),
                Arg.Any<CancellationToken>());
            await _albumRepository.Received(1).AddRangeAsync(
                Arg.Is<IEnumerable<Album>>(a => a != null && a.Count() == 1),
                Arg.Any<CancellationToken>());
            await _trackRepository.Received(1).AddRangeAsync(
                Arg.Is<IEnumerable<Track>>(t => t != null && t.Count() == 1),
                Arg.Any<CancellationToken>());
            await _listeningRepository.Received(1).AddRangeAsync(
                Arg.Is<IEnumerable<ListeningEvent>>(events => events != null && events.Count() == 1),
                Arg.Any<CancellationToken>());
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

            // Vérification : le nouvel artiste, son nouvel album ET son nouveau morceau sont indexés
            // dans le moteur de recherche en un seul appel groupé (aucun enrichissement Deezer n'est
            // planifié à l'import -- voir GetAlbumDetailUseCase/GetArtistDetailUseCase pour la
            // version à la demande).
            await _searchEnginePort.Received(1).IndexDocumentsAsync(
                Arg.Is<IEnumerable<SearchDocumentDto>>(docs => docs != null
                    && docs.Count(d => d.Type == "artist") == 1
                    && docs.Count(d => d.Type == "album") == 1
                    && docs.Count(d => d.Type == "track") == 1),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncWithDuplicateAlreadyInDatabaseShouldSkipDuplicates()
        {
            // Arrange
            var userId = Guid.NewGuid();
            DateTime listenedAt = DateTime.UtcNow.AddHours(-1);
            var isrc = new Isrc("USUM71607007");

            var existingTrack = new Track(Guid.NewGuid(), isrc, "Starboy", Guid.NewGuid(), Guid.NewGuid());

            var row = new ExcelListeningRow("Starboy", "The Weeknd", "Starboy", isrc.Value, 230, listenedAt);

            _excelParser.ParseHistoryAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns([row]);

            _trackRepository.GetByIsrcsAsync(Arg.Any<IEnumerable<Isrc>>(), Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<Track>)[existingTrack]);

            _listeningRepository.GetExistingListenedAtsAsync(userId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
                .Returns(new Dictionary<Guid, HashSet<DateTime>> { [existingTrack.Id] = [listenedAt] });

            var command = new ImportListeningHistoryCommand(userId, new MemoryStream());

            // Act
            ImportReport result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(0);
            result.SkippedCount.Should().Be(1);
            result.ErrorCount.Should().Be(0);

            await _listeningRepository.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);
            await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
            await _searchEnginePort.DidNotReceiveWithAnyArgs().IndexDocumentsAsync(default!, default);
        }

        [Fact]
        public async Task ExecuteAsyncWithDuplicateRowsWithinTheSameFileShouldCountOnlyOnce()
        {
            // Arrange : le fichier contient deux fois la même ligne (même isrc, même date d'écoute),
            // sans que rien de tel n'existe en base au préalable.
            var userId = Guid.NewGuid();
            DateTime listenedAt = DateTime.UtcNow.AddHours(-1);
            var row = new ExcelListeningRow("Starboy", "The Weeknd", "Starboy", "USUM71607007", 230, listenedAt);

            _excelParser.ParseHistoryAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns([row, row]);

            var command = new ImportListeningHistoryCommand(userId, new MemoryStream());

            // Act
            ImportReport result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(1);
            result.SkippedCount.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsyncWithInvalidIsrcShouldRecordErrorAndContinue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var invalidRow = new ExcelListeningRow("Bad Track", "Artist", "Album", "INVALID_ISRC", 180, DateTime.UtcNow);
            var validRow = new ExcelListeningRow("Good Track", "Artist", "Album", "USUM71607007", 200, DateTime.UtcNow);

            _excelParser.ParseHistoryAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns([invalidRow, validRow]);

            var command = new ImportListeningHistoryCommand(userId, new MemoryStream());

            // Act
            ImportReport result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(1);
            result.ErrorCount.Should().Be(1);
            result.Errors.Should().ContainSingle(e => e.RowNumber == 2 && e.Message.Contains("ISRC"));
        }

        [Fact]
        public async Task ExecuteAsyncWithSameArtistAcrossMultipleRowsShouldReuseArtistAndAlbumInsteadOfCreatingDuplicates()
        {
            // Arrange : deux lignes du même artiste/album (avec une casse et des espaces différents,
            // comme cela peut arriver entre deux exports Deezer), mais deux morceaux différents.
            var userId = Guid.NewGuid();
            DateTime listenedAt1 = DateTime.UtcNow.AddHours(-2);
            DateTime listenedAt2 = DateTime.UtcNow.AddHours(-1);

            var firstRow = new ExcelListeningRow("Blinding Lights", "The Weeknd", "After Hours", "USUM71607007", 200, listenedAt1);
            var secondRow = new ExcelListeningRow("In Your Eyes", " the weeknd ", "After Hours", "USUM71607008", 210, listenedAt2);

            _excelParser.ParseHistoryAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns([firstRow, secondRow]);

            var command = new ImportListeningHistoryCommand(userId, new MemoryStream());

            // Act
            ImportReport result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(2);
            result.ErrorCount.Should().Be(0);

            await _artistRepository.Received(1).AddRangeAsync(
                Arg.Is<IEnumerable<Artist>>(a => a != null && a.Count() == 1),
                Arg.Any<CancellationToken>());
            await _albumRepository.Received(1).AddRangeAsync(
                Arg.Is<IEnumerable<Album>>(a => a != null && a.Count() == 1),
                Arg.Any<CancellationToken>());
            await _trackRepository.Received(1).AddRangeAsync(
                Arg.Is<IEnumerable<Track>>(t => t != null && t.Count() == 2),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncWhenArtistAndAlbumAlreadyExistInDatabaseShouldReuseThemInsteadOfCreatingDuplicates()
        {
            // Arrange : l'artiste et l'album ont déjà été créés lors d'un import précédent.
            var userId = Guid.NewGuid();
            DateTime listenedAt = DateTime.UtcNow.AddHours(-1);

            var row = new ExcelListeningRow("New Song", "Daft Punk", "Discovery", "USUM71607009", 220, listenedAt);

            _excelParser.ParseHistoryAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns([row]);

            var existingArtist = new Artist(Guid.NewGuid(), "Daft Punk");
            _artistRepository.GetByNamesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<Artist>)[existingArtist]);

            var existingAlbum = new Album(Guid.NewGuid(), "Discovery", existingArtist.Id);
            _albumRepository.GetByArtistIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<Album>)[existingAlbum]);

            var command = new ImportListeningHistoryCommand(userId, new MemoryStream());

            // Act
            ImportReport result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(1);

            await _artistRepository.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);
            await _albumRepository.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);

            await _trackRepository.Received(1).AddRangeAsync(
                Arg.Is<IEnumerable<Track>>(tracks => tracks != null && tracks.Single().ArtistId == existingArtist.Id
                    && tracks.Single().AlbumId == existingAlbum.Id),
                Arg.Any<CancellationToken>());

            // Vérification : ni l'artiste ni l'album, déjà connus du catalogue, ne sont ré-indexés —
            // seul le nouveau morceau l'est, avec le nom du bon artiste (résolu sans aller-retour
            // base supplémentaire puisqu'il était déjà en mémoire).
            await _searchEnginePort.Received(1).IndexDocumentsAsync(
                Arg.Is<IEnumerable<SearchDocumentDto>>(docs => docs != null
                    && docs.Count() == 1
                    && docs.Single().Type == "track"
                    && docs.Single().Subtitle == existingArtist.Name),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncShouldSucceedEvenWhenSearchIndexingFails()
        {
            // Arrange : une panne du moteur de recherche (Meilisearch indisponible, etc.) ne doit
            // jamais faire échouer l'import -- Postgres, déjà persisté à ce stade, reste la source
            // de vérité ; l'index de recherche pourra être rattrapé plus tard.
            var userId = Guid.NewGuid();
            var row = new ExcelListeningRow("Starboy", "The Weeknd", "Starboy Album", "USUM71607007", 230, DateTime.UtcNow.AddHours(-1));

            _excelParser.ParseHistoryAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns([row]);

            _searchEnginePort.IndexDocumentsAsync(Arg.Any<IEnumerable<SearchDocumentDto>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new InvalidOperationException("Meilisearch indisponible.")));

            var command = new ImportListeningHistoryCommand(userId, new MemoryStream());

            // Act
            ImportReport result = await _useCase.ExecuteAsync(command);

            // Assert : l'import a quand même réussi et persisté le lot.
            result.ImportedCount.Should().Be(1);
            result.ErrorCount.Should().Be(0);
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
