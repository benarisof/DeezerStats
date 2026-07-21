using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Domain.ValueObjects;
using FluentValidation;

namespace DeezerStats.Application.UseCases.Stats
{
    public class GetTopArtistsUseCase(
        IListeningStatsQueryPort statsQueryPort,
        IValidator<GetTopArtistsQuery> validator) : IGetTopArtistsUseCase
    {
        private readonly IListeningStatsQueryPort _statsQueryPort = statsQueryPort;
        private readonly IValidator<GetTopArtistsQuery> _validator = validator;

        public async Task<PagedResult<ArtistSummary>> ExecuteAsync(GetTopArtistsQuery query, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(query, ct);

            var dateRange = new DateRange(query.From, query.To);
            return await _statsQueryPort.GetTopArtistsAsync(query.UserId, dateRange, query.Page, query.PageSize, ct);
        }
    }
}
