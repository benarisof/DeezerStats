namespace DeezerStats.Application.UseCases.Stats
{
    public record GetTopArtistsQuery(Guid UserId, DateOnly? From, DateOnly? To, int Page, int PageSize) : IPagedQuery;
}
