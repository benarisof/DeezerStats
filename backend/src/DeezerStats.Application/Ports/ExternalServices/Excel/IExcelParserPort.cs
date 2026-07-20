namespace DeezerStats.Application.Ports.ExternalServices.Excel
{
    public interface IExcelParserPort
    {
        public Task<IEnumerable<ExcelListeningRow>> ParseHistoryAsync(Stream fileStream, CancellationToken ct = default);
    }
}
