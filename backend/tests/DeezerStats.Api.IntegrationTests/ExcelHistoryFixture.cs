using System.Globalization;
using ClosedXML.Excel;

namespace DeezerStats.Api.IntegrationTests;

/// <summary>
/// Construit un vrai classeur Excel (.xlsx) en mémoire, avec la même mise en forme que le véritable
/// export Deezer "Mes données personnelles" attendue par ClosedXmlExcelParser (feuille dont le nom
/// contient "listeningHistory", en-tête en ligne 1, colonnes Song Title / Artist / ISRC / Album
/// Title / IP Address / Listening Time / Platform Name / Platform Model / Date, dans cet ordre —
/// IP Address et Platform ne sont pas exploitées, mais présentes pour rester fidèle au vrai format).
/// Toutes les valeurs sont écrites en texte, comme dans l'export réel (y compris durée et date).
/// Permet aux tests d'intégration d'exercer le vrai parseur ClosedXML plutôt qu'un port bouchonné.
/// </summary>
public static class ExcelHistoryFixture
{
    private const string _listenedAtFormat = "yyyy-MM-dd HH:mm:ss";

    public static MemoryStream Build(
        IReadOnlyList<(string TrackTitle, string ArtistName, string AlbumTitle, string Isrc, int DurationInSeconds, DateTime ListenedAt)> rows)
    {
        using var workbook = new XLWorkbook();
        IXLWorksheet worksheet = workbook.Worksheets.Add("ListeningHistory");

        worksheet.Cell(1, 1).Value = "Song Title";
        worksheet.Cell(1, 2).Value = "Artist";
        worksheet.Cell(1, 3).Value = "ISRC";
        worksheet.Cell(1, 4).Value = "Album Title";
        worksheet.Cell(1, 5).Value = "IP Address";
        worksheet.Cell(1, 6).Value = "Listening Time";
        worksheet.Cell(1, 7).Value = "Platform Name";
        worksheet.Cell(1, 8).Value = "Platform Model";
        worksheet.Cell(1, 9).Value = "Date";

        var rowIndex = 2;
        foreach ((var trackTitle, var artistName, var albumTitle, var isrc, var durationInSeconds, DateTime listenedAt) in rows)
        {
            worksheet.Cell(rowIndex, 1).Value = trackTitle;
            worksheet.Cell(rowIndex, 2).Value = artistName;
            worksheet.Cell(rowIndex, 3).Value = isrc;
            worksheet.Cell(rowIndex, 4).Value = albumTitle;
            worksheet.Cell(rowIndex, 5).Value = "127.0.0.1";
            worksheet.Cell(rowIndex, 6).Value = durationInSeconds.ToString(CultureInfo.InvariantCulture);
            worksheet.Cell(rowIndex, 7).Value = "web";
            worksheet.Cell(rowIndex, 8).Value = "None";
            worksheet.Cell(rowIndex, 9).Value = listenedAt.ToString(_listenedAtFormat, CultureInfo.InvariantCulture);
            rowIndex++;
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
