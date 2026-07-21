namespace DeezerStats.Application.DTOs.Stats
{
    public record TrackSummary(Guid Id, string Title, string ArtistName, string AlbumTitle, string? CoverUrl, int PlayCount);
}
