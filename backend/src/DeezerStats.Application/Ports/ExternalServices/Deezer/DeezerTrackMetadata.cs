using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.ExternalServices.Deezer
{
    public record DeezerTrackMetadata(string? CoverUrl, Duration Duration);
}
