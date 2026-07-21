using DeezerStats.Application.DTOs.Stats;

namespace DeezerStats.Application.UseCases.Stats
{
    public interface IGetAlbumDetailUseCase
    {
        public Task<AlbumDetail?> ExecuteAsync(GetAlbumDetailQuery query, CancellationToken ct = default);
    }
}
