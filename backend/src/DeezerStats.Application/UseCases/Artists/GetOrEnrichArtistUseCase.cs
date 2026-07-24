using DeezerStats.Application.Ports;
using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;

namespace DeezerStats.Application.UseCases.Artists
{
    /// <summary>
    /// Enrichit un artiste via l'API Deezer selon la même stratégie cache-first que
    /// <see cref="Tracks.GetOrEnrichTrackUseCase"/> et <see cref="Albums.GetOrEnrichAlbumUseCase"/> :
    /// Deezer n'est interrogé que si l'artiste n'est pas déjà enrichi (voir Artist.IsEnriched), afin
    /// de ne jamais refaire un appel réseau externe pour une donnée déjà connue en base.
    /// </summary>
    public class GetOrEnrichArtistUseCase(
        IArtistRepository artistRepository,
        IAlbumRepository albumRepository,
        IDeezerEnrichmentPort deezerPort,
        IUnitOfWork unitOfWork) : IGetOrEnrichArtistUseCase
    {
        private readonly IArtistRepository _artistRepository = artistRepository;
        private readonly IAlbumRepository _albumRepository = albumRepository;
        private readonly IDeezerEnrichmentPort _deezerPort = deezerPort;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Artist?> ExecuteAsync(GetOrEnrichArtistRequest request, CancellationToken ct = default)
        {
            Artist? artist = await _artistRepository.GetByIdAsync(request.ArtistId, ct);

            if (artist is null)
            {
                return null;
            }

            if (artist.IsEnriched)
            {
                return artist;
            }

            // Un album déjà connu de cet artiste permet à Deezer de résoudre sa couverture via le
            // lien structuré album -> artiste, bien plus fiable qu'une recherche par nom seul.
            IReadOnlyList<Album> knownAlbums = await _albumRepository.GetByArtistIdsAsync([artist.Id], ct);
            var knownAlbumTitle = knownAlbums.Count > 0 ? knownAlbums[0].Title : null;

            DeezerArtistMetadata? deezerMetadata = await _deezerPort.FetchArtistMetadataAsync(artist.Name, knownAlbumTitle, ct);

            if (deezerMetadata is not null)
            {
                artist.EnrichCover(deezerMetadata.CoverUrl);
                await _artistRepository.UpdateAsync(artist, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            return artist;
        }
    }
}
