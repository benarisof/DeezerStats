using DeezerStats.Application.DTOs.Stats;

namespace DeezerStats.Application.UseCases.Stats.TopAlbums
{
    public interface IGetTopAlbumsUseCase
    {
        public Task<PagedResult<AlbumSummary>> ExecuteAsync(GetTopAlbumsQuery query, CancellationToken ct = default);
    }
}
