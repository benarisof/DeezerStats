using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Domain.ValueObjects;
using FluentValidation;

namespace DeezerStats.Application.UseCases.Stats
{
    public class GetTopTracksUseCase(
        IListeningStatsQueryPort statsQueryPort,
        IValidator<GetTopTracksQuery> validator) : IGetTopTracksUseCase
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = statsQueryPort;
        private readonly IValidator<GetTopTracksQuery> _validator = validator;

        public async Task<PagedResult<TrackSummary>> ExecuteAsync(GetTopTracksQuery query, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(query, ct);

            var dateRange = new DateRange(query.From, query.To);
            return await _statsQueryPort.GetTopTracksAsync(query.UserId, dateRange, query.Page, query.PageSize, ct);
        }
    }
}
