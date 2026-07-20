using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports.ExternalServices.Excel;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Application.UseCases.Imports;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.Entities;
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

        private readonly ImportListeningHistoryUseCase _useCase;

        public ImportListeningHistoryUseCaseTests()
        {
            _useCase = new ImportListeningHistoryUseCase(
                _excelParser,
                _listeningRepository,
                _trackRepository,
                _artistRepository,
                _albumRepository);
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

            _listeningRepository.ExistsAsync(userId, Arg.Any<Isrc>(), listenedAt, Arg.Any<CancellationToken>())
                .Returns(false);

            _trackRepository.GetByIsrcAsync(Arg.Any<Isrc>(), Arg.Any<CancellationToken>())
                .Returns((Track?)null); // Morceau non existant en BDD

            var command = new ImportListeningHistoryCommand(userId, stream);

            // Act
            ImportResultDto result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(1);
            result.DuplicateCount.Should().Be(0);
            result.ErrorCount.Should().Be(0);

            // Vérification de la création d'artiste, album, morceau et événement d'écoute
            await _artistRepository.Received(1).AddAsync(Arg.Any<Artist>(), Arg.Any<CancellationToken>());
            await _albumRepository.Received(1).AddAsync(Arg.Any<Album>(), Arg.Any<CancellationToken>());
            await _trackRepository.Received(1).AddAsync(Arg.Any<Track>(), Arg.Any<CancellationToken>());
            await _listeningRepository.Received(1).AddRangeAsync(
                Arg.Is<IEnumerable<ListeningEvent>>(events => events != null && events.Count() == 1),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncWithDuplicateRowsShouldSkipDuplicates()
        {
            // Arrange
            var userId = Guid.NewGuid();
            DateTime listenedAt = DateTime.UtcNow.AddHours(-1);

            var row = new ExcelListeningRow("Starboy", "The Weeknd", "Starboy", "USUM71607007", 230, listenedAt);

            _excelParser.ParseHistoryAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns([row]);

            // Simule que l'écoute existe DÉJÀ en BDD
            _listeningRepository.ExistsAsync(userId, Arg.Any<Isrc>(), listenedAt, Arg.Any<CancellationToken>())
                .Returns(true);

            var command = new ImportListeningHistoryCommand(userId, new MemoryStream());

            // Act
            ImportResultDto result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(0);
            result.DuplicateCount.Should().Be(1);
            result.ErrorCount.Should().Be(0);

            // Aucun ajout ne doit être effectué
            await _listeningRepository.DidNotReceiveWithAnyArgs().AddRangeAsync(default!, default);
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

            _trackRepository.GetByIsrcAsync(Arg.Any<Isrc>(), Arg.Any<CancellationToken>())
                .Returns((Track?)null);

            var command = new ImportListeningHistoryCommand(userId, new MemoryStream());

            // Act
            ImportResultDto result = await _useCase.ExecuteAsync(command);

            // Assert
            result.ImportedCount.Should().Be(1);
            result.ErrorCount.Should().Be(1);
            result.Errors.Should().ContainSingle(e => e.RowIndex == 2 && e.Message.Contains("ISRC"));
        }
    }
}
