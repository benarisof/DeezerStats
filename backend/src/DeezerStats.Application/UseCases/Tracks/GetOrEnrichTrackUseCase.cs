using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Aggregates.TrackAggregate;

namespace DeezerStats.Application.UseCases.Tracks
{
    public class GetOrEnrichTrackUseCase(
        ITrackRepository trackRepository,
        IDeezerEnrichmentPort deezerPort)
    {
        private readonly ITrackRepository _trackRepository = trackRepository;
        private readonly IDeezerEnrichmentPort _deezerPort = deezerPort;

        public async Task<Track?> ExecuteAsync(GetOrEnrichTrackRequest request, CancellationToken ct = default)
        {
            // 1. Chercher dans Postgresql via le repository
            Track? track = await _trackRepository.GetByIsrcAsync(request.Isrc, ct);

            if (track is null)
            {
                return null;
            }

            // 2. Si le morceau est déjà enrichi, on évite un appel réseau externe
            if (track.IsEnriched)
            {
                return track;
            }

            // 3. Fallback : Appel à l'API externe Deezer
            DeezerTrackMetadata? deezerMetadata = await _deezerPort.FetchTrackMetadataAsync(track.Isrc, ct);

            if (deezerMetadata is not null)
            {
                // 4. Mutation de l'agrégat dans le domaine
                track.Enrich(deezerMetadata.Duration, deezerMetadata.CoverUrl);

                // 5. Mise en cache dans PostgreSQL
                await _trackRepository.UpdateAsync(track, ct);
            }

            return track;
        }
    }
}
