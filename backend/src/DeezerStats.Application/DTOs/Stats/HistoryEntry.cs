namespace DeezerStats.Application.DTOs.Stats
{
    public record HistoryEntry(
        Guid Id,
        Guid TrackId,
        string Title,
        string ArtistName,
        string AlbumTitle,
        string? CoverUrl,
        DateTime ListenedAt);
}
