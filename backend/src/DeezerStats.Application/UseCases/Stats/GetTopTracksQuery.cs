namespace DeezerStats.Application.UseCases.Stats
{
    public record GetTopTracksQuery(Guid UserId, DateOnly? From, DateOnly? To, int Page, int PageSize) : IPagedQuery;
}
