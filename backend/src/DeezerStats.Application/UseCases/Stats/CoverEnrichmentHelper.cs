using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Application.Ports.Catalog;

namespace DeezerStats.Application.UseCases.Stats
{
    /// <summary>
    /// Factorise, pour les trois DTO de résumé (AlbumSummary/ArtistSummary/TrackSummary), la logique
    /// partagée par les use cases de listes : repérer les éléments sans couverture, les enrichir en
    /// parallèle via <see cref="ICatalogEnrichmentCoordinator"/>, puis reporter la couverture fraîche
    /// sur la liste déjà obtenue (les autres champs -- titre, nom d'artiste, compteur d'écoutes --
    /// viennent de l'import et ne changent pas avec l'enrichissement Deezer).
    /// </summary>
    internal static class CoverEnrichmentHelper
    {
        public static async Task<IReadOnlyList<AlbumSummary>> EnrichCoversAsync(
            IReadOnlyList<AlbumSummary> items,
            ICatalogEnrichmentCoordinator coordinator,
            CancellationToken ct)
        {
            List<Guid> ids = [.. items.Where(a => a.CoverUrl is null).Select(a => a.Id)];

            if (ids.Count == 0)
            {
                return items;
            }

            IReadOnlyDictionary<Guid, string?> freshCovers = await coordinator.EnrichAlbumsAsync(ids, ct);

            return freshCovers.Count == 0
                ? items
                : [.. items.Select(a => freshCovers.TryGetValue(a.Id, out var cover) ? a with { CoverUrl = cover } : a)];
        }

        public static async Task<IReadOnlyList<ArtistSummary>> EnrichCoversAsync(
            IReadOnlyList<ArtistSummary> items,
            ICatalogEnrichmentCoordinator coordinator,
            CancellationToken ct)
        {
            List<Guid> ids = [.. items.Where(a => a.CoverUrl is null).Select(a => a.Id)];

            if (ids.Count == 0)
            {
                return items;
            }

            IReadOnlyDictionary<Guid, string?> freshCovers = await coordinator.EnrichArtistsAsync(ids, ct);

            return freshCovers.Count == 0
                ? items
                : [.. items.Select(a => freshCovers.TryGetValue(a.Id, out var cover) ? a with { CoverUrl = cover } : a)];
        }

        public static async Task<IReadOnlyList<TrackSummary>> EnrichCoversAsync(
            IReadOnlyList<TrackSummary> items,
            ICatalogEnrichmentCoordinator coordinator,
            CancellationToken ct)
        {
            List<Guid> ids = [.. items.Where(t => t.CoverUrl is null).Select(t => t.Id)];

            if (ids.Count == 0)
            {
                return items;
            }

            IReadOnlyDictionary<Guid, string?> freshCovers = await coordinator.EnrichTracksAsync(ids, ct);

            return freshCovers.Count == 0
                ? items
                : [.. items.Select(t => freshCovers.TryGetValue(t.Id, out var cover) ? t with { CoverUrl = cover } : t)];
        }
    }
}
