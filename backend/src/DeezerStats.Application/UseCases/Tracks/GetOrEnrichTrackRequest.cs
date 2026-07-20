using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.UseCases.Tracks
{
    public record GetOrEnrichTrackRequest(Isrc Isrc);
}
