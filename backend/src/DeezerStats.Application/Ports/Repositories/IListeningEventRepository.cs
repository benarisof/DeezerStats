using DeezerStats.Domain.Aggregates.ListeningEventAggregate;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.Repositories
{
    public interface IListeningEventRepository
    {
        public Task AddRangeAsync(IEnumerable<ListeningEvent> events, CancellationToken ct = default);

        public Task<bool> ExistsAsync(Guid userId, Isrc isrc, DateTime listenedAt, CancellationToken ct = default);
    }
}
