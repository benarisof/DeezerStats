using DeezerStats.Application.Ports.ExternalServices.Deezer;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Api.IntegrationTests;

/// <summary>
/// Remplace le véritable DeezerHttpEnrichmentAdapter dans les tests d'intégration (voir
/// CustomWebApplicationFactory) : ces tests exercent le pipeline HTTP réel de l'API (contrôleurs,
/// authentification, persistance...) mais ne doivent jamais dépendre d'un appel réseau sortant vers
/// l'API publique Deezer (indisponibilité, lenteur ou blocage réseau en CI rendraient les tests
/// flaky). Renvoie systématiquement "pas de métadonnée disponible", un cas déjà couvert et attendu
/// par la conception cache-first de GetOrEnrichArtistUseCase/GetOrEnrichAlbumUseCase/
/// GetOrEnrichTrackUseCase.
/// </summary>
public class FakeDeezerEnrichmentPort : IDeezerEnrichmentPort
{
    public Task<DeezerTrackMetadata?> FetchTrackMetadataAsync(Isrc isrc, CancellationToken ct = default) =>
        Task.FromResult<DeezerTrackMetadata?>(null);

    public Task<DeezerAlbumMetadata?> FetchAlbumMetadataAsync(string albumTitle, string artistName, CancellationToken ct = default) =>
        Task.FromResult<DeezerAlbumMetadata?>(null);

    public Task<DeezerArtistMetadata?> FetchArtistMetadataAsync(string artistName, CancellationToken ct = default) =>
        Task.FromResult<DeezerArtistMetadata?>(null);
}
