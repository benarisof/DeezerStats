using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Api.IntegrationTests;

/// <summary>
/// Remplace le véritable DeezerHttpEnrichmentAdapter dans les tests d'intégration (voir
/// CustomWebApplicationFactory) : ces tests exercent le pipeline HTTP réel de l'API (contrôleurs,
/// authentification, persistance...) mais ne doivent jamais dépendre d'un appel réseau sortant vers
/// l'API publique Deezer (indisponibilité, lenteur ou blocage réseau en CI rendraient les tests
/// flaky).
///
/// Par défaut, renvoie systématiquement "pas de métadonnée disponible" -- un cas déjà couvert et
/// attendu par la conception cache-first de GetOrEnrichArtistUseCase/GetOrEnrichAlbumUseCase/
/// GetOrEnrichTrackUseCase. Un test qui a besoin de simuler une réponse Deezer réelle (ex. prouver
/// qu'une cover apparaît après la première consultation d'un détail) peut résoudre cette instance
/// Singleton depuis <see cref="CustomWebApplicationFactory"/>.Services et remplacer les Func
/// ci-dessous avant d'appeler l'endpoint.
/// </summary>
public class FakeDeezerEnrichmentPort : IDeezerEnrichmentPort
{
    public Func<Isrc, DeezerTrackMetadata?> TrackMetadataFactory { get; set; } = _ => null;

    public Func<string, string, DeezerAlbumMetadata?> AlbumMetadataFactory { get; set; } = (_, _) => null;

    public Func<string, DeezerArtistMetadata?> ArtistMetadataFactory { get; set; } = _ => null;

    public Task<DeezerTrackMetadata?> FetchTrackMetadataAsync(Isrc isrc, CancellationToken ct = default) =>
        Task.FromResult(TrackMetadataFactory(isrc));

    public Task<DeezerAlbumMetadata?> FetchAlbumMetadataAsync(string albumTitle, string artistName, CancellationToken ct = default) =>
        Task.FromResult(AlbumMetadataFactory(albumTitle, artistName));

    public Task<DeezerArtistMetadata?> FetchArtistMetadataAsync(string artistName, CancellationToken ct = default) =>
        Task.FromResult(ArtistMetadataFactory(artistName));
}
