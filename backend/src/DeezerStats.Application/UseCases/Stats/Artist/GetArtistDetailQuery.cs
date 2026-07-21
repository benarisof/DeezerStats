namespace DeezerStats.Application.UseCases.Stats.Artist
{
    public record GetArtistDetailQuery(Guid UserId, Guid ArtistId, DateOnly? From, DateOnly? To);
}
