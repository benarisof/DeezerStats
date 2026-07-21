using DeezerStats.Application.UseCases.Stats;

namespace DeezerStats.Application.UseCases.Stats.TopTracks
{
    public record GetTopTracksQuery(Guid UserId, DateOnly? From, DateOnly? To, int Page, int PageSize) : IPagedQuery;
}
