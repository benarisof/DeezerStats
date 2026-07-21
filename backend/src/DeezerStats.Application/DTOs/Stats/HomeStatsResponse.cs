namespace DeezerStats.Application.DTOs.Stats
{
    public record HomeStatsResponse(
        IReadOnlyList<AlbumSummary> TopAlbums,
        IReadOnlyList<ArtistSummary> TopArtists,
        IReadOnlyList<TrackSummary> TopTracks);
}
