using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.Persistence.Repositories
{
    public class ArtistRepository(ApplicationDbContext context) : IArtistRepository
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<Artist?> GetByIdAsync(Guid id, CancellationToken ct = default) => await _context.Artists.FirstOrDefaultAsync(a => a.Id == id, ct);

        public async Task<Artist?> GetByNameAsync(string name, CancellationToken ct = default)
        {
            var normalizedName = Artist.Normalize(name);
            return await _context.Artists.FirstOrDefaultAsync(a => a.NormalizedName == normalizedName, ct);
        }

        public async Task<IReadOnlyList<Artist>> GetByNamesAsync(IEnumerable<string> names, CancellationToken ct = default)
        {
            string[] normalizedNames = [.. names.Select(Artist.Normalize).Distinct()];
            if (normalizedNames.Length == 0)
            {
                return [];
            }

            return await _context.Artists
                .Where(a => normalizedNames.Contains(a.NormalizedName))
                .ToListAsync(ct);
        }

        public async Task AddAsync(Artist artist, CancellationToken ct = default)
        {
            await _context.Artists.AddAsync(artist, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task AddRangeAsync(IEnumerable<Artist> artists, CancellationToken ct = default) => await _context.Artists.AddRangeAsync(artists, ct);

        public async Task UpdateAsync(Artist artist, CancellationToken ct = default)
        {
            _context.Artists.Update(artist);
            await _context.SaveChangesAsync(ct);
        }
    }
}
