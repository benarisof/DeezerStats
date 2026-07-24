using DeezerStats.Application.DTOs;
using DeezerStats.Application.DTOs.Search;
using DeezerStats.Application.Mappers;
using DeezerStats.Application.Ports;
using DeezerStats.Application.Ports.ExternalServices.Excel;
using DeezerStats.Application.Ports.ExternalServices.Search;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.ListeningEventAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.SeedWork;
using DeezerStats.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DeezerStats.Application.UseCases.Import
{
    public class ImportListeningHistoryUseCase(
        IExcelParserPort excelParser,
        IListeningEventRepository listeningEventRepository,
        ITrackRepository trackRepository,
        IArtistRepository artistRepository,
        IAlbumRepository albumRepository,
        IUnitOfWork unitOfWork,
        ISearchEnginePort searchEnginePort,
        ILogger<ImportListeningHistoryUseCase> logger) : IImportListeningHistoryUseCase
    {
        private static readonly Action<ILogger, int, Exception?> _logIndexingError =
            LoggerMessage.Define<int>(
                LogLevel.Error,
                new EventId(3001, "ImportSearchIndexingError"),
                "Échec de l'indexation de {DocumentCount} document(s) après l'import : import conservé, l'index sera rattrapé plus tard.");

        private readonly IExcelParserPort _excelParser = excelParser;
        private readonly IListeningEventRepository _listeningEventRepository = listeningEventRepository;
        private readonly ITrackRepository _trackRepository = trackRepository;
        private readonly IArtistRepository _artistRepository = artistRepository;
        private readonly IAlbumRepository _albumRepository = albumRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ISearchEnginePort _searchEnginePort = searchEnginePort;
        private readonly ILogger<ImportListeningHistoryUseCase> _logger = logger;

        public async Task<ImportReport> ExecuteAsync(ImportListeningHistoryCommand command, CancellationToken ct = default)
        {
            var rows = (await _excelParser.ParseHistoryAsync(command.FileStream, ct)).ToList();

            var errors = new List<ImportRowError>();

            // Valider le format ISRC en mémoire d'abord, pour ne pas gaspiller d'allers-retours en
            // base sur des lignes qui de toute façon ne pourront pas être importées.
            var validRows = new List<(int RowIndex, ExcelListeningRow Row, Isrc Isrc)>();
            var rowIndex = 1; // Ligne 1 correspond aux en-têtes Excel
            foreach (ExcelListeningRow row in rows)
            {
                rowIndex++;
                try
                {
                    validRows.Add((rowIndex, row, new Isrc(row.Isrc)));
                }
                catch (DomainException ex)
                {
                    errors.Add(new ImportRowError(rowIndex, ex.Message));
                }
                catch (Exception)
                {
                    errors.Add(new ImportRowError(rowIndex, "Format de données invalide pour cette ligne."));
                }
            }

            if (validRows.Count == 0)
            {
                return new ImportReport(0, 0, errors.Count, errors);
            }

            // Allers-retours base bornés par le nombre d'entités DISTINCTES du fichier (pas par son
            // nombre de lignes) : sur ~50 000 lignes, la différence entre quelques requêtes et des
            // dizaines de milliers.
            Isrc[] distinctIsrcs = [.. validRows.Select(r => r.Isrc).Distinct()];

            IReadOnlyList<Track> existingTracks = await _trackRepository.GetByIsrcsAsync(distinctIsrcs, ct);
            var trackByIsrc = existingTracks.ToDictionary(t => t.Isrc);

            // Par TrackId et non par ISRC : un doublon ne peut concerner qu'un morceau déjà existant,
            // un morceau tout juste créé par cet import n'ayant par définition aucune écoute antérieure.
            Guid[] existingTrackIds = [.. existingTracks.Select(t => t.Id)];
            IReadOnlyDictionary<Guid, HashSet<DateTime>> existingListenedAtsByTrackId =
                await _listeningEventRepository.GetExistingListenedAtsAsync(command.UserId, existingTrackIds, ct);

            var rowsNeedingNewTrack = validRows.Where(r => !trackByIsrc.ContainsKey(r.Isrc)).ToList();

            // Seul le premier nom de la colonne artiste (voir ParseArtistCredit) détermine
            // l'Artist/Album du morceau, pour éviter qu'un même album ne se retrouve fragmenté selon
            // les featurings ("The Weeknd" vs "The Weeknd, Future").
            var distinctArtistNames = rowsNeedingNewTrack
                .Select(r => ParseArtistCredit(r.Row.ArtistName).PrimaryArtistName)
                .Distinct()
                .ToArray();
            IReadOnlyList<Artist> existingArtists = await _artistRepository.GetByNamesAsync(distinctArtistNames, ct);
            var artistByNormalizedName = existingArtists.ToDictionary(a => a.NormalizedName);

            Guid[] existingArtistIds = [.. existingArtists.Select(a => a.Id)];
            IReadOnlyList<Album> existingAlbums = await _albumRepository.GetByArtistIdsAsync(existingArtistIds, ct);
            var albumByArtistAndNormalizedTitle =
                existingAlbums.ToDictionary(a => (a.ArtistId, a.NormalizedTitle));

            var newArtists = new Dictionary<string, Artist>();
            var newAlbums = new Dictionary<(Guid ArtistId, string NormalizedTitle), Album>();
            var newTracks = new List<Track>();
            var newEvents = new List<ListeningEvent>();
            var processedInThisImport = new HashSet<(Isrc Isrc, DateTime ListenedAt)>();
            var skippedCount = 0;

            foreach ((var index, ExcelListeningRow row, Isrc isrc) in validRows)
            {
                try
                {
                    trackByIsrc.TryGetValue(isrc, out Track? track);

                    var alreadyInDatabase = track != null
                        && existingListenedAtsByTrackId.TryGetValue(track.Id, out HashSet<DateTime>? listenedDates)
                        && listenedDates?.Contains(row.ListenedAt) == true;

                    // Empêche aussi qu'une même ligne dupliquée DANS le fichier lui-même ne soit comptée deux fois comme "importée".
                    var alreadyInThisFile = !processedInThisImport.Add((isrc, row.ListenedAt));

                    if (alreadyInDatabase || alreadyInThisFile)
                    {
                        skippedCount++;
                        continue;
                    }

                    if (track == null)
                    {
                        (var primaryArtistName, var featuredArtists) = ParseArtistCredit(row.ArtistName);
                        Artist artist = ResolveArtist(primaryArtistName, artistByNormalizedName, newArtists);
                        Album album = ResolveAlbum(row.AlbumTitle, artist.Id, albumByArtistAndNormalizedTitle, newAlbums);

                        track = new Track(Guid.NewGuid(), isrc, row.TrackTitle, artist.Id, album.Id, featuredArtists);
                        newTracks.Add(track);
                        trackByIsrc[isrc] = track; // pour les lignes suivantes référençant le même morceau
                    }

                    newEvents.Add(new ListeningEvent(
                        id: Guid.NewGuid(),
                        userId: command.UserId,
                        trackId: track.Id,
                        listeningDuration: new Duration(row.DurationInSeconds),
                        listenedAt: row.ListenedAt));
                }
                catch (DomainException ex)
                {
                    errors.Add(new ImportRowError(index, ex.Message));
                }
                catch (Exception)
                {
                    errors.Add(new ImportRowError(index, "Format de données invalide pour cette ligne."));
                }
            }

            // Un seul SaveChangesAsync pour tout le lot : garantit qu'un échec n'insère jamais un
            // artiste/album orphelin sans ses morceaux/écoutes correspondants.
            if (newArtists.Count > 0)
            {
                await _artistRepository.AddRangeAsync(newArtists.Values, ct);
            }

            if (newAlbums.Count > 0)
            {
                await _albumRepository.AddRangeAsync(newAlbums.Values, ct);
            }

            if (newTracks.Count > 0)
            {
                await _trackRepository.AddRangeAsync(newTracks, ct);
            }

            if (newEvents.Count > 0)
            {
                await _listeningEventRepository.AddRangeAsync(newEvents, ct);
            }

            if (newArtists.Count > 0 || newAlbums.Count > 0 || newTracks.Count > 0 || newEvents.Count > 0)
            {
                await _unitOfWork.SaveChangesAsync(ct);

                // L'enrichissement Deezer (cover, durée, date de sortie) n'est volontairement pas
                // déclenché ici : il se fait à la demande, lors de la consultation du détail d'un
                // album/artiste, pour ne jamais bloquer POST /imports sur des milliers d'appels
                // réseau séquentiels.
                await IndexNewCatalogEntitiesAsync(existingArtists, newArtists.Values, newAlbums.Values, newTracks, ct);
            }

            return new ImportReport(
                ImportedCount: newEvents.Count,
                SkippedCount: skippedCount,
                ErrorCount: errors.Count,
                Errors: errors);
        }

        /// <summary>
        /// Sépare la colonne artiste brute de l'export Deezer (ex. "The Weeknd, Future") en artiste
        /// principal et featurings : sans ce découpage, chaque combinaison de featuring produit un
        /// Artist/Album distinct pour un même album (incident vécu : "Hurry Up Tomorrow" fragmenté
        /// en 7 entités).
        /// </summary>
        private static (string PrimaryArtistName, string? FeaturedArtists) ParseArtistCredit(string rawArtistName)
        {
            var trimmed = rawArtistName.Trim();
            var separatorIndex = trimmed.IndexOf(", ", StringComparison.Ordinal);

            if (separatorIndex < 0)
            {
                return (trimmed, null);
            }

            var primaryArtistName = trimmed[..separatorIndex].Trim();
            var featuredArtists = trimmed[(separatorIndex + 2)..].Trim();

            return (primaryArtistName, featuredArtists.Length == 0 ? null : featuredArtists);
        }

        private static Artist ResolveArtist(
            string artistName,
            Dictionary<string, Artist> existingArtistsByNormalizedName,
            Dictionary<string, Artist> newArtistsByNormalizedName)
        {
            var normalizedName = Artist.Normalize(artistName);

            if (newArtistsByNormalizedName.TryGetValue(normalizedName, out Artist? pendingArtist))
            {
                return pendingArtist;
            }

            if (existingArtistsByNormalizedName.TryGetValue(normalizedName, out Artist? existingArtist))
            {
                return existingArtist;
            }

            var artist = new Artist(Guid.NewGuid(), artistName);
            newArtistsByNormalizedName[normalizedName] = artist;
            return artist;
        }

        private static Album ResolveAlbum(
            string albumTitle,
            Guid artistId,
            Dictionary<(Guid ArtistId, string NormalizedTitle), Album> existingAlbums,
            Dictionary<(Guid ArtistId, string NormalizedTitle), Album> newAlbums)
        {
            var normalizedTitle = Album.Normalize(albumTitle);
            (Guid ArtistId, string NormalizedTitle) key = (ArtistId: artistId, NormalizedTitle: normalizedTitle);

            if (newAlbums.TryGetValue(key, out Album? pendingAlbum))
            {
                return pendingAlbum;
            }

            if (existingAlbums.TryGetValue(key, out Album? existingAlbum))
            {
                return existingAlbum;
            }

            var album = new Album(Guid.NewGuid(), albumTitle, artistId);
            newAlbums[key] = album;
            return album;
        }

        // Une panne du moteur de recherche est journalisée puis absorbée : elle ne doit jamais faire
        // échouer l'import (Postgres, déjà persisté à ce stade, reste la source de vérité).
        private async Task IndexNewCatalogEntitiesAsync(
            IReadOnlyList<Artist> existingArtists,
            IReadOnlyCollection<Artist> newArtists,
            IReadOnlyCollection<Album> newAlbums,
            IReadOnlyCollection<Track> newTracks,
            CancellationToken ct)
        {
            // Noms d'artiste nécessaires aux documents album/morceau : déjà en mémoire, qu'ils
            // proviennent d'un artiste réutilisé (existingArtists) ou tout juste créé (newArtists) --
            // aucun aller-retour base supplémentaire.
            var artistNameById = new Dictionary<Guid, string>();

            foreach (Artist artist in existingArtists)
            {
                artistNameById[artist.Id] = artist.Name;
            }

            foreach (Artist artist in newArtists)
            {
                artistNameById[artist.Id] = artist.Name;
            }

            var documents = new List<SearchDocumentDto>();

            foreach (Artist artist in newArtists)
            {
                documents.Add(SearchMapper.ToSearchDocument(artist.Id, artist.Name, artist.CoverUrl));
            }

            foreach (Album album in newAlbums)
            {
                var artistName = artistNameById.GetValueOrDefault(album.ArtistId, string.Empty);
                documents.Add(SearchMapper.ToSearchDocument(album.Id, album.Title, artistName, album.CoverUrl));
            }

            foreach (Track track in newTracks)
            {
                var artistName = artistNameById.GetValueOrDefault(track.ArtistId, string.Empty);
                documents.Add(SearchMapper.ToSearchDocumentForTrack(track.Id, track.Title, artistName, track.CoverUrl));
            }

            if (documents.Count == 0)
            {
                return;
            }

            try
            {
                await _searchEnginePort.IndexDocumentsAsync(documents, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logIndexingError(_logger, documents.Count, ex);
            }
        }
    }
}
