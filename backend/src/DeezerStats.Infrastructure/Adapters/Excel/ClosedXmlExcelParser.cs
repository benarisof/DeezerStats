using System.Globalization;
using ClosedXML.Excel;
using DeezerStats.Application.Ports.ExternalServices.Excel;
using DeezerStats.Domain.SeedWork;

namespace DeezerStats.Infrastructure.Adapters.Excel
{
    public class ClosedXmlExcelParser : IExcelParserPort
    {
        private const string _targetSheetKeyword = "listeningHistory";

        // Ordre des colonnes de l'export "Mes données personnelles" Deezer réel (feuille
        // "10_listeningHistory") : Song Title, Artist, ISRC, Album Title, IP Address, Listening
        // Time, Platform Name, Platform Model, Date. IP Address / Platform Name / Platform Model ne
        // sont pas exploitées par le domaine.
        private const int _trackTitleColumn = 1;
        private const int _artistNameColumn = 2;
        private const int _isrcColumn = 3;
        private const int _albumTitleColumn = 4;
        private const int _durationColumn = 6;
        private const int _listenedAtColumn = 9;

        // Toutes les cellules de cette feuille (y compris les nombres et les dates) sont stockées
        // en texte brut dans l'export réel : on parse explicitement plutôt que de compter sur la
        // détection de type de ClosedXML (GetValue<T>), qui échoue sur des cellules texte.
        private const string _listenedAtFormat = "yyyy-MM-dd HH:mm:ss";

        public Task<IEnumerable<ExcelListeningRow>> ParseHistoryAsync(Stream fileStream, CancellationToken ct = default)
        {
            var result = new List<ExcelListeningRow>();

            using XLWorkbook workbook = OpenWorkbook(fileStream);

            IXLWorksheet? worksheet = workbook.Worksheets
                .FirstOrDefault(w => w.Name.Contains(_targetSheetKeyword, StringComparison.OrdinalIgnoreCase));

            worksheet ??= workbook.Worksheets.FirstOrDefault()
                ?? throw new DomainException("Le fichier Excel ne contient aucune feuille de calcul.");

            IEnumerable<IXLRow> rows = worksheet.RowsUsed().Skip(1); // ligne d'en-tête

            foreach (IXLRow row in rows)
            {
                if (row.IsEmpty())
                {
                    continue;
                }

                var trackTitle = row.Cell(_trackTitleColumn).GetString();
                var artistName = row.Cell(_artistNameColumn).GetString();
                var isrc = row.Cell(_isrcColumn).GetString();
                var albumTitle = row.Cell(_albumTitleColumn).GetString();
                var durationInSeconds = int.Parse(row.Cell(_durationColumn).GetString().Trim(), CultureInfo.InvariantCulture);

                // L'export Deezer ne porte aucune information de fuseau horaire : la colonne "Date"
                // est traitée comme déjà exprimée en UTC. Nécessaire pour Npgsql, qui refuse
                // d'écrire un DateTime Kind=Unspecified dans une colonne "timestamp with time zone"
                // (voir ListeningEventConfiguration).
                var listenedAt = DateTime.SpecifyKind(
                    DateTime.ParseExact(
                        row.Cell(_listenedAtColumn).GetString().Trim(),
                        _listenedAtFormat,
                        CultureInfo.InvariantCulture),
                    DateTimeKind.Utc);

                result.Add(new ExcelListeningRow(
                    trackTitle, artistName, albumTitle, isrc, durationInSeconds, listenedAt));
            }

            return Task.FromResult<IEnumerable<ExcelListeningRow>>(result);
        }

        private static XLWorkbook OpenWorkbook(Stream fileStream)
        {
            try
            {
                return new XLWorkbook(fileStream);
            }
            catch (Exception ex) when (ex is not DomainException)
            {
                throw new DomainException("Le fichier fourni n'est pas un classeur Excel (.xlsx) valide.", ex);
            }
        }
    }
}
