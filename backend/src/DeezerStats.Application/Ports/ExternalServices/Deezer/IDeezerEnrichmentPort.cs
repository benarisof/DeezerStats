using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.ExternalServices.Deezer
{
    public interface IDeezerEnrichmentPort
    {
        public Task<DeezerTrackMetadata?> FetchTrackMetadataAsync(Isrc isrc, CancellationToken ct = default);

        public Task<DeezerAlbumMetadata?> FetchAlbumMetadataAsync(string albumTitle, string artistName, CancellationToken ct = default);

        // knownAlbumTitle : titre d'un album déjà connu de cet artiste, s'il y en a un -- permet de
        // résoudre sa couverture via le lien structuré album -> artiste de Deezer (recherche album),
        // bien plus fiable que la recherche par nom d'artiste seul, sujette aux homonymes. Null =
        // repli direct sur la recherche par nom (voir DeezerHttpEnrichmentAdapter).
        public Task<DeezerArtistMetadata?> FetchArtistMetadataAsync(string artistName, string? knownAlbumTitle, CancellationToken ct = default);
    }
}
