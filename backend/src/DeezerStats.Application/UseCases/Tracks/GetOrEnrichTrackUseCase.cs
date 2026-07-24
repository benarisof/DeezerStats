using DeezerStats.Application.Ports;
using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Aggregates.TrackAggregate;

namespace DeezerStats.Application.UseCases.Tracks
{
    public class GetOrEnrichTrackUseCase(
        ITrackRepository trackRepository,
        IDeezerEnrichmentPort deezerPort,
        IUnitOfWork unitOfWork) : IGetOrEnrichTrackUseCase
    {
        private readonly ITrackRepository _trackRepository = trackRepository;
        private readonly IDeezerEnrichmentPort _deezerPort = deezerPort;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Track?> ExecuteAsync(GetOrEnrichTrackRequest request, CancellationToken ct = default)
        {
            Track? track = await _trackRepository.GetByIsrcAsync(request.Isrc, ct);

            return track is null ? null : await EnrichIfNeededAsync(track, ct);
        }

        public async Task<Track?> ExecuteByIdAsync(Guid trackId, CancellationToken ct = default)
        {
            Track? track = await _trackRepository.GetByIdAsync(trackId, ct);

            return track is null ? null : await EnrichIfNeededAsync(track, ct);
        }

        private async Task<Track> EnrichIfNeededAsync(Track track, CancellationToken ct)
        {
            if (track.IsEnriched)
            {
                return track;
            }

            DeezerTrackMetadata? deezerMetadata = await _deezerPort.FetchTrackMetadataAsync(track.Isrc, ct);

            if (deezerMetadata is not null)
            {
                track.Enrich(deezerMetadata.Duration, deezerMetadata.CoverUrl);
                await _trackRepository.UpdateAsync(track, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            return track;
        }
    }
}
