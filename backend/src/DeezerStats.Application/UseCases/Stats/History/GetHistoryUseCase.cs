using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Domain.ValueObjects;
using FluentValidation;

namespace DeezerStats.Application.UseCases.Stats.History
{
    public class GetHistoryUseCase(
        IListeningStatsQueryPort statsQueryPort,
        IValidator<GetHistoryQuery> validator) : IGetHistoryUseCase
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = statsQueryPort;
        private readonly IValidator<GetHistoryQuery> _validator = validator;

        public async Task<PagedResult<HistoryEntry>> ExecuteAsync(GetHistoryQuery query, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(query, ct);

            var dateRange = new DateRange(query.From, query.To);
            return await _statsQueryPort.GetHistoryAsync(query.UserId, dateRange, query.Page, query.PageSize, ct);
        }
    }
}
