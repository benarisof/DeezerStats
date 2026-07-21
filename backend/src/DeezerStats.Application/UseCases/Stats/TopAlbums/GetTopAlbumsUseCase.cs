using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Domain.ValueObjects;
using FluentValidation;

namespace DeezerStats.Application.UseCases.Stats.TopAlbums
{
    public class GetTopAlbumsUseCase(
        IListeningStatsQueryPort statsQueryPort,
        IValidator<GetTopAlbumsQuery> validator) : IGetTopAlbumsUseCase
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = statsQueryPort;
        private readonly IValidator<GetTopAlbumsQuery> _validator = validator;

        public async Task<PagedResult<AlbumSummary>> ExecuteAsync(GetTopAlbumsQuery query, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(query, ct);

            var dateRange = new DateRange(query.From, query.To);
            return await _statsQueryPort.GetTopAlbumsAsync(query.UserId, dateRange, query.Page, query.PageSize, ct);
        }
    }
}
