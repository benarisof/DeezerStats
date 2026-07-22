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

            // Étape 1 (en mémoire, aucun accès base) : valider le format ISRC de chaque ligne. Isoler
            // ici les lignes invalides évite de gaspiller des allers-retours en base sur des données
            // qui de toute façon ne pourront pas être importées.
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

            // Étapes 2 à 4 : quelques allers-retours en base (bornés par le nombre de morceaux/
            // artistes/albums DISTINCTS du fichier, pas par son nombre de lignes) au lieu d'un
            // aller-retour par ligne. Sur un fichier de ~50 000 lignes, c'est la différence entre
            // quelques requêtes et des dizaines de milliers.
            Isrc[] distinctIsrcs = [.. validRows.Select(r => r.Isrc).Distinct()];

            IReadOnlyList<Track> existingTracks = await _trackRepository.GetByIsrcsAsync(distinctIsrcs, ct);
            var trackByIsrc = existingTracks.ToDictionary(t => t.Isrc);

            // Interrogée par TrackId (et non par ISRC, voir ListeningEvent.TrackId) : un doublon en
            // base ne peut de toute façon concerner qu'un morceau déjà existant, un morceau tout
            // juste créé par cet import n'ayant par définition aucune écoute antérieure enregistrée.
            Guid[] existingTrackIds = [.. existingTracks.Select(t => t.Id)];
            IReadOnlyDictionary<Guid, HashSet<DateTime>> existingListenedAtsByTrackId =
                await _listeningEventRepository.GetExistingListenedAtsAsync(command.UserId, existingTrackIds, ct);

            var rowsNeedingNewTrack = validRows.Where(r => !trackByIsrc.ContainsKey(r.Isrc)).ToList();

            var distinctArtistNames = rowsNeedingNewTrack.Select(r => r.Row.ArtistName).Distinct().ToArray();
            IReadOnlyList<Artist> existingArtists = await _artistRepository.GetByNamesAsync(distinctArtistNames, ct);
            var artistByNormalizedName = existingArtists.ToDictionary(a => a.NormalizedName);

            Guid[] existingArtistIds = [.. existingArtists.Select(a => a.Id)];
            IReadOnlyList<Album> existingAlbums = await _albumRepository.GetByArtistIdsAsync(existingArtistIds, ct);
            var albumByArtistAndNormalizedTitle =
                existingAlbums.ToDictionary(a => (a.ArtistId, a.NormalizedTitle));

            // Étape 5 (en mémoire, aucun accès base) : construction des nouvelles entités à créer.
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

                    // Empêche aussi qu'une même ligne dupliquée deux fois DANS le fichier lui-même
                    // (par ex. export Deezer contenant deux fois la même écoute) ne soit comptée
                    // deux fois comme "importée".
                    var alreadyInThisFile = !processedInThisImport.Add((isrc, row.ListenedAt));

                    if (alreadyInDatabase || alreadyInThisFile)
                    {
                        skippedCount++;
                        continue;
                    }

                    if (track == null)
                    {
                        Artist artist = ResolveArtist(row.ArtistName, artistByNormalizedName, newArtists);
                        Album album = ResolveAlbum(row.AlbumTitle, artist.Id, albumByArtistAndNormalizedTitle, newAlbums);

                        track = new Track(Guid.NewGuid(), isrc, row.TrackTitle, artist.Id, album.Id);
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

            // Étape 6 : un seul commit atomique pour tout le lot. Les AddRangeAsync ci-dessous ne
            // font que suivre les nouvelles entités (aucun accès base individuel) ; c'est l'unique
            // appel à SaveChangesAsync qui déclenche la persistance de tout le lot en une seule
            // transaction, garantissant qu'un échec n'insère jamais un artiste/album orphelin sans
            // ses morceaux/écoutes correspondants.
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

                // Étape 7 : maintenant que le lot est persisté avec succès, on indexe les nouvelles
                // entités dans le moteur de recherche en un seul appel groupé (voir
                // ISearchEnginePort.IndexDocumentsAsync) plutôt qu'un appel par entité. L'enrichissement
                // Deezer (cover, durée, date de sortie) n'est plus déclenché ici : il se fait à la
                // demande, lors de la consultation du détail d'un album ou d'un artiste, pour ne
                // jamais bloquer la réponse HTTP de POST /imports sur des milliers d'appels réseau
                // séquentiels (voir contrat OpenAPI de /imports).
                await IndexNewCatalogEntitiesAsync(existingArtists, newArtists.Values, newAlbums.Values, newTracks, ct);
            }

            return new ImportReport(
                ImportedCount: newEvents.Count,
                SkippedCount: skippedCount,
                ErrorCount: errors.Count,
                Errors: errors);
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

        /// <summary>
        /// Construit les documents de recherche des entités nouvellement créées par cet import et
        /// les indexe en un seul appel groupé. Une panne du moteur de recherche est journalisée puis
        /// absorbée : elle ne doit jamais faire échouer l'import lui-même (Postgres, déjà persisté à
        /// ce stade, reste la source de vérité).
        /// </summary>
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
