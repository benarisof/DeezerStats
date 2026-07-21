using DeezerStats.Application.UseCases.Stats;

namespace DeezerStats.Application.UseCases.Stats.History
{
    public record GetHistoryQuery(Guid UserId, DateOnly? From, DateOnly? To, int Page, int PageSize) : IPagedQuery;
}
