namespace DeezerStats.Application.UseCases.Stats
{
    public record GetHomeStatsQuery(Guid UserId, DateOnly? From, DateOnly? To);
}
