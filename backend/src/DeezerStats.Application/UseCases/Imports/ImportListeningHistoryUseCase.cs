using DeezerStats.Application.DTOs;
using DeezerStats.Application.Ports.ExternalServices.Excel;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.ListeningEventAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.SeedWork;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.UseCases.Imports
{
    public class ImportListeningHistoryUseCase(
        IExcelParserPort excelParser,
        IListeningEventRepository listeningEventRepository,
        ITrackRepository trackRepository,
        IArtistRepository artistRepository,
        IAlbumRepository albumRepository)
    {
        private readonly IExcelParserPort _excelParser = excelParser;
        private readonly IListeningEventRepository _listeningEventRepository = listeningEventRepository;
        private readonly ITrackRepository _trackRepository = trackRepository;
        private readonly IArtistRepository _artistRepository = artistRepository;
        private readonly IAlbumRepository _albumRepository = albumRepository;

        public async Task<ImportResultDto> ExecuteAsync(ImportListeningHistoryCommand command, CancellationToken ct = default)
        {
            IEnumerable<ExcelListeningRow> rows = await _excelParser.ParseHistoryAsync(command.FileStream, ct);

            List<ListeningEvent> newEvents = [];
            List<ImportErrorDto> errors = [];
            var duplicateCount = 0;
            var rowIndex = 1; // Ligne 1 correspond aux en-têtes Excel

            foreach (ExcelListeningRow row in rows)
            {
                rowIndex++;
                try
                {
                    // 1. Validation de l'ISRC via le Value Object du Domaine
                    var isrc = new Isrc(row.Isrc);

                    // 2. Détection des doublons d'écoute (Même utilisateur + même ISRC + même date/heure)
                    var isDuplicate = await _listeningEventRepository.ExistsAsync(
                        command.UserId,
                        isrc,
                        row.ListenedAt,
                        ct);

                    if (isDuplicate)
                    {
                        duplicateCount++;
                        continue;
                    }

                    // 3. Récupération ou création des entités Catalogue (Track, Artist, Album)
                    Track track = await EnsureCatalogEntitiesExistAsync(row, isrc, ct);

                    // 4. Instanciation de l'événement d'écoute immuable
                    var listeningEvent = new ListeningEvent(
                        id: Guid.NewGuid(),
                        userId: command.UserId,
                        trackId: track.Id,
                        isrc: isrc,
                        listeningDuration: new Duration(row.DurationInSeconds),
                        listenedAt: row.ListenedAt);

                    newEvents.Add(listeningEvent);
                }
                catch (DomainException ex)
                {
                    // Capture des erreurs de validation métier (ex: ISRC invalide)
                    errors.Add(new ImportErrorDto(rowIndex, ex.Message));
                }
                catch (Exception)
                {
                    // Capture des erreurs inattendues de format de données
                    errors.Add(new ImportErrorDto(rowIndex, "Format de données invalide pour cette ligne."));
                }
            }

            // 5. Persistance en batch dans PostgreSQL
            if (newEvents.Count > 0)
            {
                await _listeningEventRepository.AddRangeAsync(newEvents, ct);
            }

            return new ImportResultDto(
                ImportedCount: newEvents.Count,
                DuplicateCount: duplicateCount,
                ErrorCount: errors.Count,
                Errors: errors);
        }

        private async Task<Track> EnsureCatalogEntitiesExistAsync(ExcelListeningRow row, Isrc isrc, CancellationToken ct)
        {
            Track? track = await _trackRepository.GetByIsrcAsync(isrc, ct);
            if (track != null)
            {
                return track;
            }

            // Si le morceau n'existe pas encore dans notre base, on initialise l'artiste et l'album associés
            var artist = new Artist(Guid.NewGuid(), row.ArtistName);
            await _artistRepository.AddAsync(artist, ct);

            var album = new Album(Guid.NewGuid(), row.AlbumTitle, artist.Id);
            await _albumRepository.AddAsync(album, ct);

            track = new Track(Guid.NewGuid(), isrc, row.TrackTitle, artist.Id, album.Id);
            await _trackRepository.AddAsync(track, ct);

            return track;
        }
    }
}
