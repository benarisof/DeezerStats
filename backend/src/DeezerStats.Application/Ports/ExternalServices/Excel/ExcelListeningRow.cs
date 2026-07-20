namespace DeezerStats.Application.Ports.ExternalServices.Excel
{
    public record ExcelListeningRow(
        string TrackTitle,
        string ArtistName,
        string AlbumTitle,
        string Isrc,
        int DurationInSeconds,
        DateTime ListenedAt);
}
