namespace DeezerStats.Application.UseCases.Stats
{
    public record GetHistoryQuery(Guid UserId, DateOnly? From, DateOnly? To, int Page, int PageSize) : IPagedQuery;
}
