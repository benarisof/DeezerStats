using DeezerStats.Application.DTOs.Stats;

namespace DeezerStats.Application.UseCases.Stats
{
    public interface IGetHistoryUseCase
    {
        public Task<PagedResult<HistoryEntry>> ExecuteAsync(GetHistoryQuery query, CancellationToken ct = default);
    }
}
