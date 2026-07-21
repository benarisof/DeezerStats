using DeezerStats.Application.DTOs.Stats;

namespace DeezerStats.Application.UseCases.Stats.TopTracks
{
    public interface IGetTopTracksUseCase
    {
        public Task<PagedResult<TrackSummary>> ExecuteAsync(GetTopTracksQuery query, CancellationToken ct = default);
    }
}
