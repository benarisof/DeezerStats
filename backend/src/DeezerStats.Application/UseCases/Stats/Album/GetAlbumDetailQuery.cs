namespace DeezerStats.Application.UseCases.Stats.Album
{
    public record GetAlbumDetailQuery(Guid UserId, Guid AlbumId, DateOnly? From, DateOnly? To);
}
