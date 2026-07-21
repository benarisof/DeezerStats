using DeezerStats.Domain.Aggregates.AlbumAggregate;

namespace DeezerStats.Application.UseCases.Albums
{
    public interface IGetOrEnrichAlbumUseCase
    {
        public Task<Album?> ExecuteAsync(GetOrEnrichAlbumRequest request, CancellationToken ct = default);
    }
}
