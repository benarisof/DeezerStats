using DeezerStats.Application.DTOs;

namespace DeezerStats.Application.UseCases.Imports
{
    public interface IImportListeningHistoryUseCase
    {
        public Task<ImportReport> ExecuteAsync(ImportListeningHistoryCommand command, CancellationToken ct = default);
    }
}
