using DeezerStats.Domain.Aggregates.AlbumAggregate;

namespace DeezerStats.Application.Ports.Repositories
{
    public interface IAlbumRepository
    {
        public Task<Album?> GetByIdAsync(Guid id, CancellationToken ct = default);

        public Task AddAsync(Album album, CancellationToken ct = default);

        public Task UpdateAsync(Album album, CancellationToken ct = default);
    }
}
