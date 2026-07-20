using ClosedXML.Excel;
using DeezerStats.Infrastructure.Adapter.Excel;
using FluentAssertions;

namespace DeezerStats.Infrastructure.UnitTests.Adapters
{
    public class ClosedXmlExcelParserTests
    {
        private readonly ClosedXmlExcelParser _parser = new();

        [Fact]
        public async Task ParseHistoryAsyncWhenMultipleSheetsExistShouldTargetListeningHistorySheet()
        {
            // Arrange : Création d'un flux Excel en mémoire avec 2 onglets
            using var stream = new MemoryStream();
            using (var workbook = new XLWorkbook())
            {
                // Onglet 1 : Feuille de résumé (à ignorer)
                IXLWorksheet summarySheet = workbook.Worksheets.Add("Résumé");
                summarySheet.Cell(1, 1).Value = "Données à ignorer";

                // Onglet 2 : Feuille cible avec le mot-clé dans le nom
                IXLWorksheet historySheet = workbook.Worksheets.Add("Export_listeningHistory_2026");

                // En-têtes
                historySheet.Cell(1, 1).Value = "Titre";
                historySheet.Cell(1, 2).Value = "Artiste";
                historySheet.Cell(1, 3).Value = "Album";
                historySheet.Cell(1, 4).Value = "ISRC";
                historySheet.Cell(1, 5).Value = "Durée";
                historySheet.Cell(1, 6).Value = "Date d'écoute";

                // Ligne de données
                historySheet.Cell(2, 1).Value = "Midnight City";
                historySheet.Cell(2, 2).Value = "M83";
                historySheet.Cell(2, 3).Value = "Hurry Up, We're Dreaming";
                historySheet.Cell(2, 4).Value = "FR6V81100021";
                historySheet.Cell(2, 5).Value = 243;
                historySheet.Cell(2, 6).Value = new DateTime(2026, 01, 15, 14, 30, 0);

                workbook.SaveAs(stream);
            }

            stream.Position = 0;

            // Act
            var result = (await _parser.ParseHistoryAsync(stream)).ToList();

            // Assert
            result.Should().HaveCount(1);
            result[0].TrackTitle.Should().Be("Midnight City");
            result[0].ArtistName.Should().Be("M83");
            result[0].Isrc.Should().Be("FR6V81100021");
            result[0].DurationInSeconds.Should().Be(243);
        }
    }
}
