using DeezerStats.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DeezerStats.Infrastructure.Persistence
{
    public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
    {
        private readonly ApplicationDbContext _context = context;

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);

        public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct = default)
        {
            if (!_context.Database.IsRelational())
            {
                // Le provider EF Core InMemory (utilisé par les tests, voir CustomWebApplicationFactory)
                // ne supporte pas les transactions explicites : chaque SaveChangesAsync y est déjà
                // atomique isolément, donc l'opération peut s'exécuter directement.
                await operation();
                return;
            }

            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                await operation();
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
    }
}
