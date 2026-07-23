using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.Persistence.Repositories
{
    public class AlbumRepository(ApplicationDbContext context) : IAlbumRepository
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<Album?> GetByIdAsync(Guid id, CancellationToken ct = default) => await _context.Albums.FirstOrDefaultAsync(a => a.Id == id, ct);

        public async Task<Album?> GetByTitleAndArtistAsync(string title, Guid artistId, CancellationToken ct = default)
        {
            var normalizedTitle = Album.Normalize(title);
            return await _context.Albums.FirstOrDefaultAsync(
                a => a.ArtistId == artistId && a.NormalizedTitle == normalizedTitle,
                ct);
        }

        public async Task<IReadOnlyList<Album>> GetByArtistIdsAsync(IEnumerable<Guid> artistIds, CancellationToken ct = default)
        {
            Guid[] ids = [.. artistIds.Distinct()];
            if (ids.Length == 0)
            {
                return [];
            }

            return await _context.Albums
                .Where(a => ids.Contains(a.ArtistId))
                .ToListAsync(ct);
        }

        public async Task AddAsync(Album album, CancellationToken ct = default) => await _context.Albums.AddAsync(album, ct);

        public async Task AddRangeAsync(IEnumerable<Album> albums, CancellationToken ct = default) => await _context.Albums.AddRangeAsync(albums, ct);

        public Task UpdateAsync(Album album, CancellationToken ct = default)
        {
            _context.Albums.Update(album);
            return Task.CompletedTask;
        }
    }
}
