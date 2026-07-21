using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.UseCases.Stats
{
    public class GetHomeStatsUseCase(IListeningStatsQueryPort statsQueryPort) : IGetHomeStatsUseCase
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = statsQueryPort;

        public async Task<HomeStatsResponse> ExecuteAsync(GetHomeStatsQuery query, CancellationToken ct = default)
        {
            var dateRange = new DateRange(query.From, query.To);
            return await _statsQueryPort.GetHomeStatsAsync(query.UserId, dateRange, ct);
        }
    }
}
