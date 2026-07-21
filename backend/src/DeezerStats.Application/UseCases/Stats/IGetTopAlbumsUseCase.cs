using DeezerStats.Application.DTOs.Stats;

namespace DeezerStats.Application.UseCases.Stats
{
    public interface IGetTopAlbumsUseCase
    {
        public Task<PagedResult<AlbumSummary>> ExecuteAsync(GetTopAlbumsQuery query, CancellationToken ct = default);
    }
}
