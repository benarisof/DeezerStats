namespace DeezerStats.Application.DTOs.Stats
{
    public record ArtistSummary(Guid Id, string Name, string? CoverUrl, int PlayCount);
}
