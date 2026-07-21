using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Application.Ports.Repositories;
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
        IDeezerEnrichmentPort deezerPort) : IGetOrEnrichArtistUseCase
    {
        private readonly IArtistRepository _artistRepository = artistRepository;
        private readonly IDeezerEnrichmentPort _deezerPort = deezerPort;

        public async Task<Artist?> ExecuteAsync(GetOrEnrichArtistRequest request, CancellationToken ct = default)
        {
            // 1. Chercher dans PostgreSQL via le repository
            Artist? artist = await _artistRepository.GetByIdAsync(request.ArtistId, ct);

            if (artist is null)
            {
                return null;
            }

            // 2. Si l'artiste est déjà enrichi, on évite un appel réseau externe
            if (artist.IsEnriched)
            {
                return artist;
            }

            // 3. Fallback : Appel à l'API externe Deezer
            DeezerArtistMetadata? deezerMetadata = await _deezerPort.FetchArtistMetadataAsync(artist.Name, ct);

            if (deezerMetadata is not null)
            {
                // 4. Mutation de l'agrégat dans le domaine
                artist.EnrichCover(deezerMetadata.CoverUrl);

                // 5. Persistance des métadonnées dans PostgreSQL
                await _artistRepository.UpdateAsync(artist, ct);
            }

            return artist;
        }
    }
}
