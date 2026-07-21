using ClosedXML.Excel;

namespace DeezerStats.Api.IntegrationTests;

/// <summary>
/// Construit un vrai classeur Excel (.xlsx) en mémoire, avec la même mise en forme que celle
/// attendue par ClosedXmlExcelParser (feuille dont le nom contient "listeningHistory", en-tête en
/// ligne 1, colonnes Track/Artist/Album/ISRC/Duration/ListenedAt dans cet ordre). Permet aux tests
/// d'intégration d'exercer le vrai parseur ClosedXML plutôt qu'un port bouchonné.
/// </summary>
public static class ExcelHistoryFixture
{
    public static MemoryStream Build(
        IReadOnlyList<(string TrackTitle, string ArtistName, string AlbumTitle, string Isrc, int DurationInSeconds, DateTime ListenedAt)> rows)
    {
        using var workbook = new XLWorkbook();
        IXLWorksheet worksheet = workbook.Worksheets.Add("ListeningHistory");

        worksheet.Cell(1, 1).Value = "Track";
        worksheet.Cell(1, 2).Value = "Artist";
        worksheet.Cell(1, 3).Value = "Album";
        worksheet.Cell(1, 4).Value = "ISRC";
        worksheet.Cell(1, 5).Value = "Duration";
        worksheet.Cell(1, 6).Value = "ListenedAt";

        var rowIndex = 2;
        foreach ((var trackTitle, var artistName, var albumTitle, var isrc, var durationInSeconds, DateTime listenedAt) in rows)
        {
            worksheet.Cell(rowIndex, 1).Value = trackTitle;
            worksheet.Cell(rowIndex, 2).Value = artistName;
            worksheet.Cell(rowIndex, 3).Value = albumTitle;
            worksheet.Cell(rowIndex, 4).Value = isrc;
            worksheet.Cell(rowIndex, 5).Value = durationInSeconds;
            worksheet.Cell(rowIndex, 6).Value = listenedAt;
            rowIndex++;
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
