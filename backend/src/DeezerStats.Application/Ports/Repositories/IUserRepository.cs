using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.Repositories
{
    public interface IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

        public Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);

        public Task AddAsync(User user, CancellationToken ct = default);
    }
}
