using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Aggregates.ListeningEventAggregate;
using DeezerStats.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.Persistence.Repositories;

public class ListeningEventRepository(ApplicationDbContext context) : IListeningEventRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task AddRangeAsync(IEnumerable<ListeningEvent> events, CancellationToken ct = default)
    {
        await _context.ListeningEvents.AddRangeAsync(events, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid userId, Isrc isrc, DateTime listenedAt, CancellationToken ct = default)
    {
        return await _context.ListeningEvents.AnyAsync(
            e => e.UserId == userId && e.Isrc == isrc && e.ListenedAt == listenedAt,
            ct);
    }
}
