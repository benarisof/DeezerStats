namespace DeezerStats.Application.DTOs.Stats
{
    public record AlbumSummary(Guid Id, string Title, string ArtistName, string? CoverUrl, int PlayCount);
}
