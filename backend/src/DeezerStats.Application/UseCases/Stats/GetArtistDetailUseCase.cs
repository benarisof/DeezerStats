using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.UseCases.Stats
{
    public class GetArtistDetailUseCase(IListeningStatsQueryPort statsQueryPort) : IGetArtistDetailUseCase
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = statsQueryPort;

        public async Task<ArtistDetail?> ExecuteAsync(GetArtistDetailQuery query, CancellationToken ct = default)
        {
            var dateRange = new DateRange(query.From, query.To);
            return await _statsQueryPort.GetArtistDetailAsync(query.UserId, query.ArtistId, dateRange, ct);
        }
    }
}
