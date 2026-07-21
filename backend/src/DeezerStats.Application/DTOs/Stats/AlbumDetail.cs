namespace DeezerStats.Application.DTOs.Stats
{
    public record AlbumDetail(
        Guid Id,
        string Title,
        Guid ArtistId,
        string ArtistName,
        string? CoverUrl,
        int? DurationSeconds,
        DateOnly? ReleaseDate,
        double TotalListeningDurationHours,
        int TotalPlayCount,
        IReadOnlyList<AlbumTrackItem> Tracks);
}
