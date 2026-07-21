using DeezerStats.Application.DTOs.Stats;

namespace DeezerStats.Application.UseCases.Stats.Home
{
    public interface IGetHomeStatsUseCase
    {
        public Task<HomeStatsResponse> ExecuteAsync(GetHomeStatsQuery query, CancellationToken ct = default);
    }
}
