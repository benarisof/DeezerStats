namespace DeezerStats.Application.UseCases.Stats
{
    public record GetArtistDetailQuery(Guid UserId, Guid ArtistId, DateOnly? From, DateOnly? To);
}
