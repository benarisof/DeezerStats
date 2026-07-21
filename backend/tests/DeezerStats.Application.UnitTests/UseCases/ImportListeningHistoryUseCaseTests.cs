using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports;
using DeezerStats.Application.Ports.ExternalServices.Excel;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.UseCases.Imports;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.ListeningEventAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;
using FluentAssertions;
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

        private readonly ImportListeningHistoryUseCase _useCase;

        public ImportListeningHistoryUseCaseTests()
        {
            _useCase = new ImportListeningHistoryUseCase(
                _excelParser,
                _listeningRepository,
                _trackRepository,
                _artistRepository,
                _albumRepository,
                _unitOfWork);

            // Par défaut, aucune donnée préexistante en base : chaque test ne surcharge que ce dont
            // il a besoin pour rester lisible.
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
            ImportResultDto result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(1);
            result.DuplicateCount.Should().Be(0);
            result.ErrorCount.Should().Be(0);

            // Vérification de la création d'artiste, album, morceau et événement d'écoute, en UN
            // seul appel "AddRangeAsync" par type d'entité (pas un appel par ligne), suivi d'un
            // unique commit atomique.
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
        }

        [Fact]
        public async Task ExecuteAsyncWithDuplicateAlreadyInDatabaseShouldSkipDuplicates()
        {
            // Arrange
            var userId = Guid.NewGuid();
            DateTime listenedAt = DateTime.UtcNow.AddHours(-1);
            var isrc = new Isrc("USUM71607007");

            // Le morceau existe déjà en base (import précédent) : un doublon en base ne peut de
            // toute façon concerner qu'un morceau déjà connu (voir ImportListeningHistoryUseCase).
            var existingTrack = new Track(Guid.NewGuid(), isrc, "Starboy", Guid.NewGuid(), Guid.NewGuid());

            var row = new ExcelListeningRow("Starboy", "The Weeknd", "Starboy", isrc.Value, 230, listenedAt);

            _excelParser.ParseHistoryAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns([row]);

            _trackRepository.GetByIsrcsAsync(Arg.Any<IEnumerable<Isrc>>(), Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<Track>)[existingTrack]);

            // Simule que cette écoute (morceau + date) existe déjà en base pour cet utilisateur.
            _listeningRepository.GetExistingListenedAtsAsync(userId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
                .Returns(new Dictionary<Guid, HashSet<DateTime>> { [existingTrack.Id] = [listenedAt] });

            var command = new ImportListeningHistoryCommand(userId, new MemoryStream());

            // Act
            ImportResultDto result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(0);
            result.DuplicateCount.Should().Be(1);
            result.ErrorCount.Should().Be(0);

            // Rien à persister : aucun appel de lot, et surtout aucun commit inutile.
            await _listeningRepository.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);
            await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
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
            ImportResultDto result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(1);
            result.DuplicateCount.Should().Be(1);
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
            ImportResultDto result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(1);
            result.ErrorCount.Should().Be(1);
            result.Errors.Should().ContainSingle(e => e.RowIndex == 2 && e.Message.Contains("ISRC"));
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
            ImportResultDto result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(2);
            result.ErrorCount.Should().Be(0);

            // Un seul artiste et un seul album doivent avoir été créés malgré les deux lignes, et
            // malgré la casse/les espaces différents dans le nom d'artiste ; en revanche deux
            // morceaux distincts.
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
            ImportResultDto result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(1);

            // L'artiste et l'album existants doivent être réutilisés, jamais recréés.
            await _artistRepository.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);
            await _albumRepository.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);

            await _trackRepository.Received(1).AddRangeAsync(
                Arg.Is<IEnumerable<Track>>(tracks => tracks != null && tracks.Single().ArtistId == existingArtist.Id
                    && tracks.Single().AlbumId == existingAlbum.Id),
                Arg.Any<CancellationToken>());
        }
    }
}
