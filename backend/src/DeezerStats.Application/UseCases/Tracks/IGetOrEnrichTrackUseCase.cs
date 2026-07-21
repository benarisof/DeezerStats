using DeezerStats.Domain.Aggregates.TrackAggregate;

namespace DeezerStats.Application.UseCases.Tracks
{
    public interface IGetOrEnrichTrackUseCase
    {
        public Task<Track?> ExecuteAsync(GetOrEnrichTrackRequest request, CancellationToken ct = default);
    }
}
