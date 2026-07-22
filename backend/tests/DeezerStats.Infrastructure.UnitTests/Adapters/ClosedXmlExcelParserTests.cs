using ClosedXML.Excel;
using DeezerStats.Infrastructure.Adapters.Excel;
using FluentAssertions;

namespace DeezerStats.Infrastructure.UnitTests.Adapters
{
    public class ClosedXmlExcelParserTests
    {
        private readonly ClosedXmlExcelParser _parser = new();

        [Fact]
        public async Task ParseHistoryAsyncWhenMultipleSheetsExistShouldTargetListeningHistorySheet()
        {
            // Arrange : Création d'un flux Excel en mémoire avec 2 onglets, reproduisant le format
            // réel de l'export Deezer "Mes données personnelles" (feuille "10_listeningHistory") :
            // Song Title, Artist, ISRC, Album Title, IP Address, Listening Time, Platform Name,
            // Platform Model, Date -- toutes les valeurs en texte, y compris durée et date.
            using var stream = new MemoryStream();
            using (var workbook = new XLWorkbook())
            {
                // Onglet 1 : Feuille de résumé (à ignorer)
                IXLWorksheet summarySheet = workbook.Worksheets.Add("Résumé");
                summarySheet.Cell(1, 1).Value = "Données à ignorer";

                // Onglet 2 : Feuille cible avec le mot-clé dans le nom
                IXLWorksheet historySheet = workbook.Worksheets.Add("Export_listeningHistory_2026");

                // En-têtes (format réel Deezer)
                historySheet.Cell(1, 1).Value = "Song Title";
                historySheet.Cell(1, 2).Value = "Artist";
                historySheet.Cell(1, 3).Value = "ISRC";
                historySheet.Cell(1, 4).Value = "Album Title";
                historySheet.Cell(1, 5).Value = "IP Address";
                historySheet.Cell(1, 6).Value = "Listening Time";
                historySheet.Cell(1, 7).Value = "Platform Name";
                historySheet.Cell(1, 8).Value = "Platform Model";
                historySheet.Cell(1, 9).Value = "Date";

                // Ligne de données
                historySheet.Cell(2, 1).Value = "Midnight City";
                historySheet.Cell(2, 2).Value = "M83";
                historySheet.Cell(2, 3).Value = "FR6V81100021";
                historySheet.Cell(2, 4).Value = "Hurry Up, We're Dreaming";
                historySheet.Cell(2, 5).Value = "81.57.92.39";
                historySheet.Cell(2, 6).Value = "243";
                historySheet.Cell(2, 7).Value = "web";
                historySheet.Cell(2, 8).Value = "None";
                historySheet.Cell(2, 9).Value = "2026-01-15 14:30:00";

                workbook.SaveAs(stream);
            }

            stream.Position = 0;

            // Act
            var result = (await _parser.ParseHistoryAsync(stream)).ToList();

            // Assert
            result.Should().HaveCount(1);
            result[0].TrackTitle.Should().Be("Midnight City");
            result[0].ArtistName.Should().Be("M83");
            result[0].AlbumTitle.Should().Be("Hurry Up, We're Dreaming");
            result[0].Isrc.Should().Be("FR6V81100021");
            result[0].DurationInSeconds.Should().Be(243);
            result[0].ListenedAt.Should().Be(new DateTime(2026, 01, 15, 14, 30, 0));
        }

        [Fact]
        public async Task ParseHistoryAsyncShouldParseNegativeDurationAsIs()
        {
            // Arrange : l'export réel Deezer utilise "-1" pour une durée d'écoute inconnue/skippée
            // (voir ImportListeningHistoryUseCase, qui rejette ensuite cette ligne via Duration --
            // une durée négative n'est pas valide côté domaine -- sans faire échouer tout l'import).
            using var stream = new MemoryStream();
            using (var workbook = new XLWorkbook())
            {
                IXLWorksheet sheet = workbook.Worksheets.Add("10_listeningHistory");

                sheet.Cell(1, 1).Value = "Song Title";
                sheet.Cell(1, 2).Value = "Artist";
                sheet.Cell(1, 3).Value = "ISRC";
                sheet.Cell(1, 4).Value = "Album Title";
                sheet.Cell(1, 5).Value = "IP Address";
                sheet.Cell(1, 6).Value = "Listening Time";
                sheet.Cell(1, 7).Value = "Platform Name";
                sheet.Cell(1, 8).Value = "Platform Model";
                sheet.Cell(1, 9).Value = "Date";

                sheet.Cell(2, 1).Value = "Say What You Say";
                sheet.Cell(2, 2).Value = "Eminem";
                sheet.Cell(2, 3).Value = "USIR10211047";
                sheet.Cell(2, 4).Value = "The Eminem Show";
                sheet.Cell(2, 5).Value = "81.57.92.39";
                sheet.Cell(2, 6).Value = "-1";
                sheet.Cell(2, 7).Value = "None";
                sheet.Cell(2, 8).Value = "None";
                sheet.Cell(2, 9).Value = "2009-11-07 16:35:11";

                workbook.SaveAs(stream);
            }

            stream.Position = 0;

            // Act
            var result = (await _parser.ParseHistoryAsync(stream)).ToList();

            // Assert
            result.Should().HaveCount(1);
            result[0].DurationInSeconds.Should().Be(-1);
        }
    }
}
