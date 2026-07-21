using DeezerStats.Application.DTOs.Stats;

namespace DeezerStats.Application.UseCases.Stats
{
    public interface IGetArtistDetailUseCase
    {
        public Task<ArtistDetail?> ExecuteAsync(GetArtistDetailQuery query, CancellationToken ct = default);
    }
}
