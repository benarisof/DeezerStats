using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.ExternalServices.Deezer
{
    public interface IDeezerEnrichmentPort
    {
        public Task<DeezerTrackMetadata?> FetchTrackMetadataAsync(Isrc isrc, CancellationToken ct = default);

        public Task<DeezerAlbumMetadata?> FetchAlbumMetadataAsync(string albumTitle, string artistName, CancellationToken ct = default);

        public Task<DeezerArtistMetadata?> FetchArtistMetadataAsync(string artistName, CancellationToken ct = default);
    }
}
