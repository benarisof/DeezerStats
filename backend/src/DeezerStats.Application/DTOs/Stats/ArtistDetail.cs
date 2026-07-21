namespace DeezerStats.Application.DTOs.Stats
{
    /// <summary>
    /// Adapté par rapport au cahier des charges initial : les champs "durée" et "date de sortie"
    /// d'un album n'ont pas de sens pour un artiste. Remplacés par des agrégats propres à l'artiste
    /// (voir ADR ticket 1.1 / 2.3, et openapi.yaml).
    /// </summary>
    public record ArtistDetail(
        Guid Id,
        string Name,
        string? CoverUrl,
        int DistinctAlbumsCount,
        int DistinctTracksCount,
        double TotalListeningDurationHours,
        int TotalPlayCount,
        IReadOnlyList<ArtistTrackItem> Tracks);
}
