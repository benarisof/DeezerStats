using DeezerStats.Domain.Entities;

namespace DeezerStats.Application.Ports.Repositories
{
    public interface IArtistRepository
    {
        public Task<Artist?> GetByIdAsync(Guid id, CancellationToken ct = default);

        public Task AddAsync(Artist artist, CancellationToken ct = default);
    }
}
