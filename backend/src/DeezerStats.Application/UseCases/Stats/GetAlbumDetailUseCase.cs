using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.UseCases.Stats
{
    public class GetAlbumDetailUseCase(IListeningStatsQueryPort statsQueryPort) : IGetAlbumDetailUseCase
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = statsQueryPort;

        public async Task<AlbumDetail?> ExecuteAsync(GetAlbumDetailQuery query, CancellationToken ct = default)
        {
            var dateRange = new DateRange(query.From, query.To);
            return await _statsQueryPort.GetAlbumDetailAsync(query.UserId, query.AlbumId, dateRange, ct);
        }
    }
}
