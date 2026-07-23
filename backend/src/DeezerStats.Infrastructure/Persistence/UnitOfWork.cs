using DeezerStats.Application.Ports;

namespace DeezerStats.Infrastructure.Persistence
{
    public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
    {
        private readonly ApplicationDbContext _context = context;

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
    }
}
