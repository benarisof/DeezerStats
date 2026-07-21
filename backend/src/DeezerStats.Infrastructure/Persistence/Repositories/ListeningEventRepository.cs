using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Aggregates.ListeningEventAggregate;
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

    public async Task<bool> ExistsAsync(Guid userId, Guid trackId, DateTime listenedAt, CancellationToken ct = default)
    {
        return await _context.ListeningEvents.AnyAsync(
            e => e.UserId == userId && e.TrackId == trackId && e.ListenedAt == listenedAt,
            ct);
    }

    public async Task<IReadOnlyDictionary<Guid, HashSet<DateTime>>> GetExistingListenedAtsAsync(
        Guid userId,
        IEnumerable<Guid> trackIds,
        CancellationToken ct = default)
    {
        List<Guid> trackIdList = trackIds.Distinct().ToList();
        if (trackIdList.Count == 0)
        {
            return new Dictionary<Guid, HashSet<DateTime>>();
        }

        List<ListeningEvent> existingEvents = await _context.ListeningEvents
            .Where(e => e.UserId == userId && trackIdList.Contains(e.TrackId))
            .ToListAsync(ct);

        var result = new Dictionary<Guid, HashSet<DateTime>>();
        foreach (ListeningEvent listeningEvent in existingEvents)
        {
            if (!result.TryGetValue(listeningEvent.TrackId, out HashSet<DateTime>? dates))
            {
                dates = [];
                result[listeningEvent.TrackId] = dates;
            }

            dates.Add(listeningEvent.ListenedAt);
        }

        return result;
    }
}
