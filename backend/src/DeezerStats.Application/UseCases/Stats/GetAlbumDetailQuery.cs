namespace DeezerStats.Application.UseCases.Stats
{
    public record GetAlbumDetailQuery(Guid UserId, Guid AlbumId, DateOnly? From, DateOnly? To);
}
