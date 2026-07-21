using DeezerStats.Application.DTOs;

namespace DeezerStats.Application.UseCases.Import
{
    public interface IImportListeningHistoryUseCase
    {
        public Task<ImportReport> ExecuteAsync(ImportListeningHistoryCommand command, CancellationToken ct = default);
    }
}
