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
    /// Point d'attention EF Core : Duration est un Value Object converti vers un int (secondes) via
    /// DomainValueConverters. EF Core sait traduire l'accès à la propriété convertie elle-même (ex.
    /// "le.ListeningDuration") en SQL, mais PAS un accès membre supplémentaire dessus (ex.
    /// "le.ListeningDuration.TotalSeconds") : ce type d'accès ne peut être traduit en SQL. Les
    /// sommes de durée (voir BuildTrackListeningStatsAsync) sont donc calculées en mémoire, après
    /// matérialisation des ListeningDuration bruts -- volumétrie bornée par le nombre de morceaux
    /// d'un seul album/artiste, donc sans enjeu de performance.
    ///
    /// Les classements (tops albums/artistes/morceaux), en revanche, sont désormais groupés/comptés/
    /// triés/plafonnés côté SQL (GROUP BY + COUNT + ORDER BY + LIMIT), et non plus en mémoire.
    /// L'échec de traduction initialement rencontré ("The LINQ expression ... could not be
    /// translated") ne venait pas du GROUP BY lui-même, mais de la projection finale vers un type
    /// applicatif nommé (ex. "new AlbumSummary(...)") : EF Core sait traduire un GROUP BY projeté
    /// vers un type ANONYME (voir Build*SummariesAsync ci-dessous), le mapping vers le record nommé
    /// se faisant ensuite en mémoire sur un résultat déjà réduit au strict nécessaire (Take). Vérifié
    /// contre la vraie base (pas seulement le provider EF Core InMemory, qui ne reproduit pas cette
    /// limite de traduction) avant ce changement.
    /// </summary>
    public class ListeningStatsQueryService(ApplicationDbContext context) : IListeningStatsQueryPort
    {
        /// <summary>
        /// Nombre d'éléments par catégorie affichés sur la page d'accueil (voir HomeStatsResponse) :
        /// volontairement demandé au plus près du besoin (10) plutôt que le plafond général des tops
        /// (StatsRules.MaxRankedResults, 100), pour que la requête SQL sous-jacente (GROUP BY + LIMIT)
        /// n'agrège que ce qui sera effectivement affiché.
        /// </summary>
        private const int _homeStatsItemCount = 10;

        private readonly ApplicationDbContext _context = context;

        public async Task<HomeStatsResponse> GetHomeStatsAsync(Guid userId, DateRange dateRange, CancellationToken ct = default)
        {
            (DateTime? fromDate, DateTime? toDate) = ToInclusiveBounds(dateRange);

            // Une seule requête SQL par catégorie (contre une matérialisation complète de l'historique
            // de l'utilisateur x3 auparavant) : chacune reste néanmoins séquentielle, le DbContext
            // n'étant pas thread-safe pour des requêtes concurrentes sur la même instance (voir
            // CatalogEnrichmentCoordinator pour le pattern utilisé quand une vraie parallélisation est
            // nécessaire, avec un DbContext isolé par tâche).
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
        ///
        /// DateOnly.ToDateTime produit systématiquement un DateTimeKind.Unspecified, alors que
        /// ListenedAt est mappée en "timestamp with time zone" (voir ApplicationDbContext) : Npgsql
        /// refuse d'écrire un DateTime Unspecified dans ce type de colonne ("Cannot write DateTime
        /// with Kind=Unspecified [...], only UTC is supported"), ce qui faisait échouer en 500 tout
        /// appel aux endpoints de consultation dès qu'un filtre from/to était fourni -- même bug que
        /// celui déjà rencontré et corrigé côté import (voir ClosedXmlExcelParser).
        /// </summary>
        private static (DateTime? From, DateTime? To) ToInclusiveBounds(DateRange dateRange) =>
            (ToUtc(dateRange.From, TimeOnly.MinValue), ToUtc(dateRange.To, TimeOnly.MaxValue));

        private static DateTime? ToUtc(DateOnly? date, TimeOnly time) =>
            date.HasValue ? DateTime.SpecifyKind(date.Value.ToDateTime(time), DateTimeKind.Utc) : null;

        /// <summary>
        /// Plafonne un classement déjà trié (voir StatsRules.MaxRankedResults) puis en extrait la
        /// page demandée. Purement en mémoire, mais sur un résultat déjà plafonné côté SQL par les
        /// méthodes Build*SummariesAsync (le ".Take(StatsRules.MaxRankedResults)" ci-dessous ne fait
        /// donc jamais rien en pratique) : conservé comme garde-fou défensif plutôt que retiré, au cas
        /// où un appelant futur oublierait de plafonner sa requête en amont.
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
        /// trié par nombre d'écoutes décroissant et plafonné à <paramref name="take"/> résultats --
        /// jointure, regroupement, comptage, tri ET plafonnage sont tous traduits en SQL (voir la
        /// remarque en tête de fichier), Postgres ne renvoyant jamais plus de lignes que nécessaire.
        /// </summary>
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

        /// <summary>
        /// Construit le classement des albums (avec nombre d'écoutes cumulé sur tous leurs morceaux)
        /// pour un utilisateur donné, plafonné à <paramref name="take"/> résultats. Voir la remarque
        /// de <see cref="BuildTrackSummariesAsync"/>.
        /// </summary>
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

        /// <summary>
        /// Construit le classement des artistes (avec nombre d'écoutes cumulé sur tous leurs
        /// morceaux, tous albums confondus) pour un utilisateur donné, plafonné à
        /// <paramref name="take"/> résultats. Voir la remarque de <see cref="BuildTrackSummariesAsync"/>.
        /// </summary>
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
