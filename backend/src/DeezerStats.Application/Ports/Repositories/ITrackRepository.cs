using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.Repositories
{
    public interface ITrackRepository
    {
        public Task<Track?> GetByIdAsync(Guid id, CancellationToken ct = default);

        public Task<Track?> GetByIsrcAsync(Isrc isrc, CancellationToken ct = default);

        public Task AddAsync(Track track, CancellationToken ct = default);

        public Task UpdateAsync(Track track, CancellationToken ct = default);
    }
}
