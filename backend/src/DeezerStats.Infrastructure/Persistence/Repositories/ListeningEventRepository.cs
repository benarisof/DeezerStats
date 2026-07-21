using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Entities;
using DeezerStats.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.Persistence.Repositories;

public class ListeningEventRepository(ApplicationDbContext context) : IListeningEventRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task AddRangeAsync(IEnumerable<ListeningEvent> events, CancellationToken ct = default)
    {
        // NB : ne déclenche plus SaveChangesAsync (voir IListeningEventRepository.AddRangeAsync) :
        // l'appelant (ex. ImportListeningHistoryUseCase) doit committer explicitement via
        // IUnitOfWork, une fois que toutes les entités du lot (artistes, albums, morceaux, écoutes)
        // ont été ajoutées au suivi du contexte, pour une persistance atomique en une seule fois.
        await _context.ListeningEvents.AddRangeAsync(events, ct);
    }

    public async Task<bool> ExistsAsync(Guid userId, Isrc isrc, DateTime listenedAt, CancellationToken ct = default)
    {
        return await _context.ListeningEvents.AnyAsync(
            e => e.UserId == userId && e.Isrc == isrc && e.ListenedAt == listenedAt,
            ct);
    }

    public async Task<IReadOnlyDictionary<Isrc, HashSet<DateTime>>> GetExistingListenedAtsAsync(
        Guid userId,
        IEnumerable<Isrc> isrcs,
        CancellationToken ct = default)
    {
        List<Isrc> isrcList = isrcs.Distinct().ToList();
        if (isrcList.Count == 0)
        {
            return new Dictionary<Isrc, HashSet<DateTime>>();
        }

        List<ListeningEvent> existingEvents = await _context.ListeningEvents
            .Where(e => e.UserId == userId && isrcList.Contains(e.Isrc))
            .ToListAsync(ct);

        var result = new Dictionary<Isrc, HashSet<DateTime>>();
        foreach (ListeningEvent listeningEvent in existingEvents)
        {
            if (!result.TryGetValue(listeningEvent.Isrc, out HashSet<DateTime>? dates))
            {
                dates = [];
                result[listeningEvent.Isrc] = dates;
            }

            dates.Add(listeningEvent.ListenedAt);
        }

        return result;
    }
}
