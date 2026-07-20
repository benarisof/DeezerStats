using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.ExternalServices.Deezer
{
    public record DeezerAlbumMetadata(string? CoverUrl, DateOnly? ReleaseDate, Duration? Duration);
}
