using DeezerStats.Application.Ports;
using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;

namespace DeezerStats.Application.UseCases.Albums
{
    /// <summary>
    /// Enrichit un album via l'API Deezer selon la même stratégie cache-first que
    /// <see cref="Tracks.GetOrEnrichTrackUseCase"/> : Deezer n'est interrogé que si l'album n'est
    /// pas déjà entièrement enrichi (cover + date de sortie + durée), afin de ne jamais refaire un
    /// appel réseau externe pour une donnée déjà connue en base.
    /// </summary>
    public class GetOrEnrichAlbumUseCase(
        IAlbumRepository albumRepository,
        IArtistRepository artistRepository,
        IDeezerEnrichmentPort deezerPort,
        IUnitOfWork unitOfWork) : IGetOrEnrichAlbumUseCase
    {
        private readonly IAlbumRepository _albumRepository = albumRepository;
        private readonly IArtistRepository _artistRepository = artistRepository;
        private readonly IDeezerEnrichmentPort _deezerPort = deezerPort;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Album?> ExecuteAsync(GetOrEnrichAlbumRequest request, CancellationToken ct = default)
        {
            // 1. Chercher dans PostgreSQL via le repository
            Album? album = await _albumRepository.GetByIdAsync(request.AlbumId, ct);

            if (album is null)
            {
                return null;
            }

            // 2. Si l'album est déjà enrichi, on évite un appel réseau externe
            if (album.IsEnriched)
            {
                return album;
            }

            // 3. L'API Deezer se recherche par titre d'album + nom d'artiste (voir
            // IDeezerEnrichmentPort.FetchAlbumMetadataAsync) : il faut donc résoudre l'artiste.
            Artist? artist = await _artistRepository.GetByIdAsync(album.ArtistId, ct);

            if (artist is null)
            {
                // Situation anormale (album orphelin), mais ne doit pas faire échouer l'enrichissement
                // des autres éléments de la file : on renvoie l'album tel quel, non enrichi.
                return album;
            }

            // 4. Fallback : Appel à l'API externe Deezer
            DeezerAlbumMetadata? deezerMetadata = await _deezerPort.FetchAlbumMetadataAsync(album.Title, artist.Name, ct);

            if (deezerMetadata is not null)
            {
                // 5. Mutation de l'agrégat dans le domaine
                album.Enrich(deezerMetadata.CoverUrl, deezerMetadata.ReleaseDate, deezerMetadata.Duration);

                // 6. Persistance des métadonnées dans PostgreSQL (UpdateAsync ne fait que suivre le
                // changement, voir IAlbumRepository.UpdateAsync -- SaveChangesAsync déclenche
                // l'écriture réelle).
                await _albumRepository.UpdateAsync(album, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            return album;
        }
    }
}
