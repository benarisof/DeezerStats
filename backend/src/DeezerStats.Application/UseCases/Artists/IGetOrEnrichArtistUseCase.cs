using DeezerStats.Domain.Aggregates.ArtistAggregate;

namespace DeezerStats.Application.UseCases.Artists
{
    public interface IGetOrEnrichArtistUseCase
    {
        public Task<Artist?> ExecuteAsync(GetOrEnrichArtistRequest request, CancellationToken ct = default);
    }
}
