using ClosedXML.Excel;
using DeezerStats.Application.Ports.ExternalServices.Excel;
using DeezerStats.Domain.SeedWork;

namespace DeezerStats.Infrastructure.Adapters.Excel
{
    public class ClosedXmlExcelParser : IExcelParserPort
    {
        private const string _targetSheetKeyword = "listeningHistory";

        public Task<IEnumerable<ExcelListeningRow>> ParseHistoryAsync(Stream fileStream, CancellationToken ct = default)
        {
            var result = new List<ExcelListeningRow>();

            using XLWorkbook workbook = OpenWorkbook(fileStream);

            // 1. Recherche de la feuille dont le nom contient "listeningHistory" (insensible à la casse)
            IXLWorksheet? worksheet = workbook.Worksheets
                .FirstOrDefault(w => w.Name.Contains(_targetSheetKeyword, StringComparison.OrdinalIgnoreCase));

            // 2. Repli : Si aucune feuille ne correspond, on prend la première par défaut (ou on lève une exception)
            worksheet ??= workbook.Worksheets.FirstOrDefault()
                ?? throw new DomainException("Le fichier Excel ne contient aucune feuille de calcul.");

            IEnumerable<IXLRow> rows = worksheet.RowsUsed().Skip(1); // Sauter la ligne d'en-tête

            foreach (IXLRow row in rows)
            {
                // Vérification que la ligne n'est pas vide
                if (row.IsEmpty())
                {
                    continue;
                }

                var trackTitle = row.Cell(1).GetValue<string>();
                var artistName = row.Cell(2).GetValue<string>();
                var albumTitle = row.Cell(3).GetValue<string>();
                var isrc = row.Cell(4).GetValue<string>();
                var durationInSeconds = row.Cell(5).GetValue<int>();
                DateTime listenedAt = row.Cell(6).GetValue<DateTime>();

                result.Add(new ExcelListeningRow(
                    trackTitle, artistName, albumTitle, isrc, durationInSeconds, listenedAt));
            }

            return Task.FromResult<IEnumerable<ExcelListeningRow>>(result);
        }

        // ClosedXML lève des types d'exception internes (format ZIP invalide, fichier corrompu,
        // pas un .xlsx du tout...) quand le flux fourni n'est pas un classeur Excel exploitable.
        // Sans cette traduction, une telle erreur atterrirait dans le catch-all du middleware (500),
        // alors que le contrat OpenAPI documente un 400 "Fichier illisible" pour POST /imports.
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
