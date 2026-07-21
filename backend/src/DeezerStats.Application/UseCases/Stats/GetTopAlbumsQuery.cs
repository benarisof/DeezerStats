namespace DeezerStats.Application.UseCases.Stats
{
    public record GetTopAlbumsQuery(Guid UserId, DateOnly? From, DateOnly? To, int Page, int PageSize) : IPagedQuery;
}
