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
    /// <see cref="IListeningStatsQueryPort"/>). Regroupe les jointures ListeningEvent -> Track ->
    /// Album/Artist et les agrégations (COUNT, GROUP BY) nécessaires aux endpoints de consultation
    /// (Phase 9).
    ///
    /// Deux points d'attention EF Core, tous deux résolus en calculant en mémoire plutôt qu'en SQL :
    ///
    /// 1. Duration est un Value Object converti vers un int (secondes) via DomainValueConverters. EF
    ///    Core sait traduire l'accès à la propriété convertie elle-même (ex. "le.ListeningDuration")
    ///    en SQL, mais PAS un accès membre supplémentaire dessus (ex.
    ///    "le.ListeningDuration.TotalSeconds") : ce type d'accès ne peut être traduit en SQL. Les
    ///    sommes de durée sont donc systématiquement calculées en mémoire, après matérialisation
    ///    (ToListAsync) des ListeningDuration bruts.
    ///
    /// 2. Un classement (tops albums/artistes/morceaux) nécessite une jointure ListeningEvent ->
    ///    Track -> Album/Artist SUIVIE d'un GROUP BY + COUNT projeté dans un type applicatif (pas un
    ///    type anonyme). EF Core échoue systématiquement à traduire cette combinaison précise
    ///    ("The LINQ expression ... could not be translated"), y compris quand la jointure est faite
    ///    avant le GroupBy (pas de composition de requête entre méthodes) : c'est une limite du
    ///    traducteur de requêtes lui-même, pas un problème de style d'écriture. Le classement est
    ///    donc construit en récupérant les lignes jointes/filtrées via ToListAsync (une seule requête
    ///    SQL simple, sans agrégation), puis en groupant/comptant/triant en mémoire -- volumétrie
    ///    largement raisonnable pour l'historique d'écoute d'un seul utilisateur.
    /// </summary>
    public class ListeningStatsQueryService(ApplicationDbContext context) : IListeningStatsQueryPort
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<HomeStatsResponse> GetHomeStatsAsync(Guid userId, DateRange dateRange, CancellationToken ct = default)
        {
            (DateTime? fromDate, DateTime? toDate) = ToInclusiveBounds(dateRange);

            List<AlbumSummary> topAlbums = [.. (await BuildAlbumSummariesAsync(userId, fromDate, toDate, ct)).Take(10)];
            List<ArtistSummary> topArtists = [.. (await BuildArtistSummariesAsync(userId, fromDate, toDate, ct)).Take(10)];
            List<TrackSummary> topTracks = [.. (await BuildTrackSummariesAsync(userId, fromDate, toDate, ct)).Take(10)];

            return new HomeStatsResponse(topAlbums, topArtists, topTracks);
        }

        public async Task<PagedResult<AlbumSummary>> GetTopAlbumsAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default)
        {
            (DateTime? fromDate, DateTime? toDate) = ToInclusiveBounds(dateRange);
            List<AlbumSummary> summaries = await BuildAlbumSummariesAsync(userId, fromDate, toDate, ct);
            return ToPagedResult(summaries, page, pageSize);
        }

        public async Task<PagedResult<ArtistSummary>> GetTopArtistsAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default)
        {
            (DateTime? fromDate, DateTime? toDate) = ToInclusiveBounds(dateRange);
            List<ArtistSummary> summaries = await BuildArtistSummariesAsync(userId, fromDate, toDate, ct);
            return ToPagedResult(summaries, page, pageSize);
        }

        public async Task<PagedResult<TrackSummary>> GetTopTracksAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default)
        {
            (DateTime? fromDate, DateTime? toDate) = ToInclusiveBounds(dateRange);
            List<TrackSummary> summaries = await BuildTrackSummariesAsync(userId, fromDate, toDate, ct);
            return ToPagedResult(summaries, page, pageSize);
        }

        public async Task<PagedResult<HistoryEntry>> GetHistoryAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default)
        {
            (DateTime? fromDate, DateTime? toDate) = ToInclusiveBounds(dateRange);

            // Remarque : les bornes sont nommées fromDate/toDate (et non from/to) car "from" est un
            // mot-clé contextuel de la syntaxe de requête LINQ ci-dessous -- l'utiliser comme nom de
            // variable référencé À L'INTÉRIEUR d'une expression "from...where...select" fait échouer
            // la compilation (CS1525 "Invalid expression term").
            //
            // Contrairement aux tops (voir BuildTrackSummariesAsync et consorts), l'historique ne
            // fait aucun GROUP BY : une simple jointure + tri est fiablement traduite en SQL, donc
            // la pagination reste faite côté base (Take/Skip/CountAsync).
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

            // distinctAlbumsCount/distinctTracksCount reflètent uniquement ce que CET utilisateur a
            // réellement écouté (playCount > 0) sur la plage demandée : le catalogue (Track/Album/
            // Artist) est global et partagé entre utilisateurs (dédupliqué par nom), il ne reflète
            // pas "tout ce que l'artiste a sorti" mais "ce qui a été importé par un import quelconque".
            // Seul le nombre d'écoutes de l'utilisateur courant a un sens en tant que statistique
            // personnelle.
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

        /// <summary>
        /// Convertit la plage de dates métier (DateOnly, bornes incluses) en bornes DateTime
        /// utilisables directement dans une comparaison avec ListeningEvent.ListenedAt : le début
        /// de la journée pour "from", la fin de la journée (23:59:59.9999999) pour "to" afin
        /// d'inclure toute la journée de fin, conformément à la sémantique "incluse" du contrat OpenAPI.
        /// </summary>
        private static (DateTime? From, DateTime? To) ToInclusiveBounds(DateRange dateRange) =>
            (dateRange.From?.ToDateTime(TimeOnly.MinValue), dateRange.To?.ToDateTime(TimeOnly.MaxValue));

        /// <summary>
        /// Plafonne un classement déjà trié (voir StatsRules.MaxRankedResults) puis en extrait la
        /// page demandée. Purement en mémoire : les listes en entrée proviennent des méthodes
        /// Build*SummariesAsync, déjà matérialisées côté base via une seule requête SQL simple.
        /// </summary>
        private static PagedResult<T> ToPagedResult<T>(IReadOnlyList<T> orderedItems, int page, int pageSize)
        {
            IReadOnlyList<T> capped = orderedItems.Count > StatsRules.MaxRankedResults
                ? [.. orderedItems.Take(StatsRules.MaxRankedResults)]
                : orderedItems;

            List<T> items = [.. capped.Skip((page - 1) * pageSize).Take(pageSize)];
            return new PagedResult<T>(items, page, pageSize, capped.Count);
        }

        /// <summary>
        /// Construit le classement des morceaux (avec nombre d'écoutes) pour un utilisateur donné,
        /// trié par nombre d'écoutes décroissant. Voir la remarque en tête de fichier sur le GROUP BY
        /// non traduisible par EF Core dans ce contexte : la jointure et le filtrage sont faits en
        /// SQL (une requête simple, sans agrégation), le regroupement/comptage/tri en mémoire.
        /// </summary>
        private async Task<List<TrackSummary>> BuildTrackSummariesAsync(Guid userId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
        {
            var rows = await (
                from le in _context.ListeningEvents
                where le.UserId == userId
                    && (fromDate == null || le.ListenedAt >= fromDate)
                    && (toDate == null || le.ListenedAt <= toDate)
                join t in _context.Tracks on le.TrackId equals t.Id
                join al in _context.Albums on t.AlbumId equals al.Id
                join ar in _context.Artists on t.ArtistId equals ar.Id
                select new { TrackId = t.Id, t.Title, ArtistName = ar.Name, AlbumTitle = al.Title, t.CoverUrl })
                .ToListAsync(ct);

            return [.. rows
                .GroupBy(x => x.TrackId)
                .Select(g => new TrackSummary(g.Key, g.First().Title, g.First().ArtistName, g.First().AlbumTitle, g.First().CoverUrl, g.Count()))
                .OrderByDescending(s => s.PlayCount)];
        }

        /// <summary>
        /// Construit le classement des albums (avec nombre d'écoutes cumulé sur tous leurs morceaux)
        /// pour un utilisateur donné. Voir la remarque de <see cref="BuildTrackSummariesAsync"/>.
        /// </summary>
        private async Task<List<AlbumSummary>> BuildAlbumSummariesAsync(Guid userId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
        {
            var rows = await (
                from le in _context.ListeningEvents
                where le.UserId == userId
                    && (fromDate == null || le.ListenedAt >= fromDate)
                    && (toDate == null || le.ListenedAt <= toDate)
                join t in _context.Tracks on le.TrackId equals t.Id
                join al in _context.Albums on t.AlbumId equals al.Id
                join ar in _context.Artists on al.ArtistId equals ar.Id
                select new { AlbumId = al.Id, al.Title, ArtistName = ar.Name, al.CoverUrl })
                .ToListAsync(ct);

            return [.. rows
                .GroupBy(x => x.AlbumId)
                .Select(g => new AlbumSummary(g.Key, g.First().Title, g.First().ArtistName, g.First().CoverUrl, g.Count()))
                .OrderByDescending(s => s.PlayCount)];
        }

        /// <summary>
        /// Construit le classement des artistes (avec nombre d'écoutes cumulé sur tous leurs
        /// morceaux, tous albums confondus) pour un utilisateur donné. Voir la remarque de
        /// <see cref="BuildTrackSummariesAsync"/>.
        /// </summary>
        private async Task<List<ArtistSummary>> BuildArtistSummariesAsync(Guid userId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
        {
            var rows = await (
                from le in _context.ListeningEvents
                where le.UserId == userId
                    && (fromDate == null || le.ListenedAt >= fromDate)
                    && (toDate == null || le.ListenedAt <= toDate)
                join t in _context.Tracks on le.TrackId equals t.Id
                join ar in _context.Artists on t.ArtistId equals ar.Id
                select new { ArtistId = ar.Id, ar.Name, ar.CoverUrl })
                .ToListAsync(ct);

            return [.. rows
                .GroupBy(x => x.ArtistId)
                .Select(g => new ArtistSummary(g.Key, g.First().Name, g.First().CoverUrl, g.Count()))
                .OrderByDescending(s => s.PlayCount)];
        }

        /// <summary>
        /// Nombre d'écoutes et durée totale écoutée (en secondes) par morceau, pour un utilisateur
        /// et un ensemble de morceaux donnés (utilisé par les pages item album/artiste). La durée
        /// est sommée en mémoire après matérialisation (voir la note en tête de fichier sur
        /// ListeningDuration.TotalSeconds), la sélection de "le.ListeningDuration" seule (sans accès
        /// membre) restant traduisible par EF Core.
        /// </summary>
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
