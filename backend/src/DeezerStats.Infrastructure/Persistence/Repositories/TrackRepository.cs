using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.Persistence.Repositories
{
    public class TrackRepository(ApplicationDbContext context) : ITrackRepository
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<Track?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _context.Tracks.FirstOrDefaultAsync(t => t.Id == id, ct);

        public async Task<Track?> GetByIsrcAsync(Isrc isrc, CancellationToken ct = default)
            => await _context.Tracks.FirstOrDefaultAsync(t => t.Isrc == isrc, ct);

        public async Task<IReadOnlyList<Track>> GetByIsrcsAsync(IEnumerable<Isrc> isrcs, CancellationToken ct = default)
        {
            List<Isrc> isrcList = isrcs.Distinct().ToList();
            if (isrcList.Count == 0)
            {
                return [];
            }

            return await _context.Tracks
                .Where(t => isrcList.Contains(t.Isrc))
                .ToListAsync(ct);
        }

        public async Task AddAsync(Track track, CancellationToken ct = default)
        {
            await _context.Tracks.AddAsync(track, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task AddRangeAsync(IEnumerable<Track> tracks, CancellationToken ct = default)
        {
            await _context.Tracks.AddRangeAsync(tracks, ct);
        }

        public async Task UpdateAsync(Track track, CancellationToken ct = default)
        {
            _context.Tracks.Update(track);
            await _context.SaveChangesAsync(ct);
        }
    }
}
