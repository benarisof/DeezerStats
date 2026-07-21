using DeezerStats.Application.DTOs.Stats;

namespace DeezerStats.Application.UseCases.Stats.TopArtists
{
    public interface IGetTopArtistsUseCase
    {
        public Task<PagedResult<ArtistSummary>> ExecuteAsync(GetTopArtistsQuery query, CancellationToken ct = default);
    }
}
