using DeezerStats.Application.Common;
using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Queries;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.Persistence.Queries
{
    /// <summary>
    /// Implémentation EF Core du port de lecture des statistiques d'écoute (voir
    /// <see cref="IListeningStatsQueryPort"/>).
    ///
    /// Deux pièges EF Core à connaître avant de modifier ce fichier : (1) Duration est converti en
    /// int via DomainValueConverters -- EF Core traduit "le.ListeningDuration" en SQL mais pas un
    /// accès membre dessus ("le.ListeningDuration.TotalSeconds"), d'où les sommes de durée calculées
    /// en mémoire (BuildTrackListeningStatsAsync). (2) un GROUP BY se traduit en SQL seulement si la
    /// projection finale reste anonyme -- projeter directement vers un record nommé (ex.
    /// "new AlbumSummary(...)") fait échouer la traduction ("The LINQ expression ... could not be
    /// translated") ; le mapping vers le record se fait donc en mémoire, après le GROUP BY/Take.
    /// </summary>
    public class ListeningStatsQueryService(ApplicationDbContext context) : IListeningStatsQueryPort
    {
        // Plafond volontairement plus bas que StatsRules.MaxRankedResults (100) : la page d'accueil
        // n'affiche que 10 éléments par catégorie.
        private const int _homeStatsItemCount = 10;

        private readonly ApplicationDbContext _context = context;

        public async Task<HomeStatsResponse> GetHomeStatsAsync(Guid userId, DateRange dateRange, CancellationToken ct = default)
        {
            (DateTime? fromDate, DateTime? toDate) = ToInclusiveBounds(dateRange);

            // Séquentiel (pas Task.WhenAll) : le DbContext n'est pas thread-safe pour des requêtes
            // concurrentes sur la même instance.
            List<AlbumSummary> topAlbums = await BuildAlbumSummariesAsync(userId, fromDate, toDate, _homeStatsItemCount, ct);
            List<ArtistSummary> topArtists = await BuildArtistSummariesAsync(userId, fromDate, toDate, _homeStatsItemCount, ct);
            List<TrackSummary> topTracks = await BuildTrackSummariesAsync(userId, fromDate, toDate, _homeStatsItemCount, ct);

            return new HomeStatsResponse(topAlbums, topArtists, topTracks);
        }

        public async Task<PagedResult<AlbumSummary>> GetTopAlbumsAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default)
        {
            (DateTime? fromDate, DateTime? toDate) = ToInclusiveBounds(dateRange);
            List<AlbumSummary> summaries = await BuildAlbumSummariesAsync(userId, fromDate, toDate, StatsRules.MaxRankedResults, ct);
            return ToPagedResult(summaries, page, pageSize);
        }

        public async Task<PagedResult<ArtistSummary>> GetTopArtistsAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default)
        {
            (DateTime? fromDate, DateTime? toDate) = ToInclusiveBounds(dateRange);
            List<ArtistSummary> summaries = await BuildArtistSummariesAsync(userId, fromDate, toDate, StatsRules.MaxRankedResults, ct);
            return ToPagedResult(summaries, page, pageSize);
        }

        public async Task<PagedResult<TrackSummary>> GetTopTracksAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default)
        {
            (DateTime? fromDate, DateTime? toDate) = ToInclusiveBounds(dateRange);
            List<TrackSummary> summaries = await BuildTrackSummariesAsync(userId, fromDate, toDate, StatsRules.MaxRankedResults, ct);
            return ToPagedResult(summaries, page, pageSize);
        }

        public async Task<PagedResult<HistoryEntry>> GetHistoryAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default)
        {
            (DateTime? fromDate, DateTime? toDate) = ToInclusiveBounds(dateRange);

            // Nommées fromDate/toDate et non from/to : "from" est un mot-clé contextuel de la syntaxe
            // de requête LINQ ci-dessous, l'utiliser comme nom de variable ferait échouer la
            // compilation (CS1525).
            IQueryable<HistoryEntry> query =
                from le in _context.ListeningEvents
                where le.UserId == userId
                    && (fromDate == null || le.ListenedAt >= fromDate)
                    && (toDate == null || le.ListenedAt <= toDate)
                join t in _context.Tracks on le.TrackId equals t.Id
                join al in _context.Albums on t.AlbumId equals al.Id
                join ar in _context.Artists on t.ArtistId equals ar.Id
                orderby le.ListenedAt descending
                select new HistoryEntry(le.Id, t.Id, t.Title, ar.Name, al.Title, t.CoverUrl, le.ListenedAt);

            IQueryable<HistoryEntry> capped = query.Take(StatsRules.MaxRankedResults);
            var totalItems = await capped.CountAsync(ct);
            List<HistoryEntry> items = await capped.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return new PagedResult<HistoryEntry>(items, page, pageSize, totalItems);
        }

        public async Task<AlbumDetail?> GetAlbumDetailAsync(Guid userId, Guid albumId, DateRange dateRange, CancellationToken ct = default)
        {
            var albumWithArtist = await (
                from al in _context.Albums
                where al.Id == albumId
                join ar in _context.Artists on al.ArtistId equals ar.Id
                select new { Album = al, ArtistName = ar.Name })
                .FirstOrDefaultAsync(ct);

            if (albumWithArtist is null)
            {
                return null;
            }

            List<Track> tracks = await _context.Tracks.Where(t => t.AlbumId == albumId).ToListAsync(ct);

            (DateTime? fromDate, DateTime? toDate) = ToInclusiveBounds(dateRange);
            Dictionary<Guid, (int PlayCount, int TotalSeconds)> statsByTrack =
                await BuildTrackListeningStatsAsync(userId, [.. tracks.Select(t => t.Id)], fromDate, toDate, ct);

            List<AlbumTrackItem> trackItems = [.. tracks
                .Select(t =>
                {
                    (var playCount, var _) = statsByTrack.TryGetValue(t.Id, out (int PlayCount, int TotalSeconds) stats) ? stats : (0, 0);
                    return new AlbumTrackItem(t.Id, t.Title, playCount);
                })
                .OrderByDescending(item => item.PlayCount)];

            var totalPlayCount = statsByTrack.Values.Sum(s => s.PlayCount);
            var totalListeningDurationHours = statsByTrack.Values.Sum(s => s.TotalSeconds) / 3600.0;

            Album album = albumWithArtist.Album;
            return new AlbumDetail(
                album.Id,
                album.Title,
                album.ArtistId,
                albumWithArtist.ArtistName,
                album.CoverUrl,
                album.Duration?.TotalSeconds,
                album.ReleaseDate,
                totalListeningDurationHours,
                totalPlayCount,
                trackItems);
        }

        public async Task<ArtistDetail?> GetArtistDetailAsync(Guid userId, Guid artistId, DateRange dateRange, CancellationToken ct = default)
        {
            Artist? artist = await _context.Artists.FirstOrDefaultAsync(a => a.Id == artistId, ct);
            if (artist is null)
            {
                return null;
            }

            var tracksWithAlbum = await (
                from t in _context.Tracks
                where t.ArtistId == artistId
                join al in _context.Albums on t.AlbumId equals al.Id
                select new { Track = t, AlbumTitle = al.Title })
                .ToListAsync(ct);

            (DateTime? fromDate, DateTime? toDate) = ToInclusiveBounds(dateRange);
            Dictionary<Guid, (int PlayCount, int TotalSeconds)> statsByTrack =
                await BuildTrackListeningStatsAsync(userId, [.. tracksWithAlbum.Select(x => x.Track.Id)], fromDate, toDate, ct);

            List<ArtistTrackItem> trackItems = [.. tracksWithAlbum
                .Select(x =>
                {
                    (var playCount, var _) = statsByTrack.TryGetValue(x.Track.Id, out (int PlayCount, int TotalSeconds) stats) ? stats : (0, 0);
                    return new ArtistTrackItem(x.Track.Id, x.Track.Title, x.AlbumTitle, playCount);
                })
                .OrderByDescending(item => item.PlayCount)];

            // distinctAlbumsCount/distinctTracksCount ne comptent que ce que CET utilisateur a
            // réellement écouté (playCount > 0) : le catalogue est global et partagé entre
            // utilisateurs, il ne reflète pas la discographie complète de l'artiste.
            HashSet<Guid> playedTrackIds = [.. statsByTrack.Where(kvp => kvp.Value.PlayCount > 0).Select(kvp => kvp.Key)];
            var distinctTracksCount = playedTrackIds.Count;
            var distinctAlbumsCount = tracksWithAlbum
                .Where(x => playedTrackIds.Contains(x.Track.Id))
                .Select(x => x.Track.AlbumId)
                .Distinct()
                .Count();

            var totalPlayCount = statsByTrack.Values.Sum(s => s.PlayCount);
            var totalListeningDurationHours = statsByTrack.Values.Sum(s => s.TotalSeconds) / 3600.0;

            return new ArtistDetail(
                artist.Id,
                artist.Name,
                artist.CoverUrl,
                distinctAlbumsCount,
                distinctTracksCount,
                totalListeningDurationHours,
                totalPlayCount,
                trackItems);
        }

        // "to" va jusqu'à 23:59:59.9999999 pour inclure toute la journée de fin (sémantique "bornes
        // incluses" du contrat OpenAPI).
        private static (DateTime? From, DateTime? To) ToInclusiveBounds(DateRange dateRange) =>
            (ToUtc(dateRange.From, TimeOnly.MinValue), ToUtc(dateRange.To, TimeOnly.MaxValue));

        // DateOnly.ToDateTime produit un DateTimeKind.Unspecified ; Npgsql refuse d'écrire ce Kind
        // dans une colonne "timestamp with time zone" (ListenedAt), d'où la conversion explicite en UTC.
        private static DateTime? ToUtc(DateOnly? date, TimeOnly time) =>
            date.HasValue ? DateTime.SpecifyKind(date.Value.ToDateTime(time), DateTimeKind.Utc) : null;

        // Le plafonnage ci-dessous est redondant avec celui déjà fait côté SQL par les méthodes
        // Build*SummariesAsync ; conservé comme garde-fou si un futur appelant l'omettait en amont.
        private static PagedResult<T> ToPagedResult<T>(IReadOnlyList<T> orderedItems, int page, int pageSize)
        {
            IReadOnlyList<T> capped = orderedItems.Count > StatsRules.MaxRankedResults
                ? [.. orderedItems.Take(StatsRules.MaxRankedResults)]
                : orderedItems;

            List<T> items = [.. capped.Skip((page - 1) * pageSize).Take(pageSize)];
            return new PagedResult<T>(items, page, pageSize, capped.Count);
        }

        private async Task<List<TrackSummary>> BuildTrackSummariesAsync(Guid userId, DateTime? fromDate, DateTime? toDate, int take, CancellationToken ct)
        {
            var rows = await (
                from le in _context.ListeningEvents
                where le.UserId == userId
                    && (fromDate == null || le.ListenedAt >= fromDate)
                    && (toDate == null || le.ListenedAt <= toDate)
                join t in _context.Tracks on le.TrackId equals t.Id
                join al in _context.Albums on t.AlbumId equals al.Id
                join ar in _context.Artists on t.ArtistId equals ar.Id
                group le by new { t.Id, t.Title, ArtistName = ar.Name, AlbumTitle = al.Title, t.CoverUrl } into g
                select new
                {
                    g.Key.Id,
                    g.Key.Title,
                    g.Key.ArtistName,
                    g.Key.AlbumTitle,
                    g.Key.CoverUrl,
                    PlayCount = g.Count(),
                })
                .OrderByDescending(x => x.PlayCount)
                .Take(take)
                .ToListAsync(ct);

            return [.. rows.Select(r => new TrackSummary(r.Id, r.Title, r.ArtistName, r.AlbumTitle, r.CoverUrl, r.PlayCount))];
        }

        private async Task<List<AlbumSummary>> BuildAlbumSummariesAsync(Guid userId, DateTime? fromDate, DateTime? toDate, int take, CancellationToken ct)
        {
            var rows = await (
                from le in _context.ListeningEvents
                where le.UserId == userId
                    && (fromDate == null || le.ListenedAt >= fromDate)
                    && (toDate == null || le.ListenedAt <= toDate)
                join t in _context.Tracks on le.TrackId equals t.Id
                join al in _context.Albums on t.AlbumId equals al.Id
                join ar in _context.Artists on al.ArtistId equals ar.Id
                group le by new { al.Id, al.Title, ArtistName = ar.Name, al.CoverUrl } into g
                select new
                {
                    g.Key.Id,
                    g.Key.Title,
                    g.Key.ArtistName,
                    g.Key.CoverUrl,
                    PlayCount = g.Count(),
                })
                .OrderByDescending(x => x.PlayCount)
                .Take(take)
                .ToListAsync(ct);

            return [.. rows.Select(r => new AlbumSummary(r.Id, r.Title, r.ArtistName, r.CoverUrl, r.PlayCount))];
        }

        private async Task<List<ArtistSummary>> BuildArtistSummariesAsync(Guid userId, DateTime? fromDate, DateTime? toDate, int take, CancellationToken ct)
        {
            var rows = await (
                from le in _context.ListeningEvents
                where le.UserId == userId
                    && (fromDate == null || le.ListenedAt >= fromDate)
                    && (toDate == null || le.ListenedAt <= toDate)
                join t in _context.Tracks on le.TrackId equals t.Id
                join ar in _context.Artists on t.ArtistId equals ar.Id
                group le by new { ar.Id, ar.Name, ar.CoverUrl } into g
                select new
                {
                    g.Key.Id,
                    g.Key.Name,
                    g.Key.CoverUrl,
                    PlayCount = g.Count(),
                })
                .OrderByDescending(x => x.PlayCount)
                .Take(take)
                .ToListAsync(ct);

            return [.. rows.Select(r => new ArtistSummary(r.Id, r.Name, r.CoverUrl, r.PlayCount))];
        }

        private async Task<Dictionary<Guid, (int PlayCount, int TotalSeconds)>> BuildTrackListeningStatsAsync(
            Guid userId, IReadOnlyCollection<Guid> trackIds, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
        {
            if (trackIds.Count == 0)
            {
                return [];
            }

            var events = await _context.ListeningEvents
                .Where(le => le.UserId == userId
                    && trackIds.Contains(le.TrackId)
                    && (fromDate == null || le.ListenedAt >= fromDate)
                    && (toDate == null || le.ListenedAt <= toDate))
                .Select(le => new { le.TrackId, le.ListeningDuration })
                .ToListAsync(ct);

            return events
                .GroupBy(e => e.TrackId)
                .ToDictionary(g => g.Key, g => (g.Count(), g.Sum(e => e.ListeningDuration.TotalSeconds)));
        }
    }
}
