namespace DeezerStats.Application.UseCases.Stats.Home
{
    public record GetHomeStatsQuery(Guid UserId, DateOnly? From, DateOnly? To);
}
