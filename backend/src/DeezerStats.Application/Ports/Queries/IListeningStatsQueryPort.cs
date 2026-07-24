using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.Queries
{
    /// <summary>
    /// Port de lecture pour les statistiques d'écoute (page d'accueil, tops, historique, pages
    /// item album/artiste). Distinct des repositories (Application.Ports.Repositories, orientés
    /// écriture/agrégat) : ces requêtes traversent plusieurs agrégats (ListeningEvent -> Track ->
    /// Album/Artist) avec des agrégations qui n'ont pas leur place dans un repository par agrégat.
    /// Toutes les méthodes sont scopées par utilisateur : les statistiques d'un utilisateur ne
    /// doivent jamais fuiter vers un autre.
    /// </summary>
    public interface IListeningStatsQueryPort
    {
        public Task<HomeStatsResponse> GetHomeStatsAsync(Guid userId, DateRange dateRange, CancellationToken ct = default);

        public Task<PagedResult<AlbumSummary>> GetTopAlbumsAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default);

        public Task<PagedResult<ArtistSummary>> GetTopArtistsAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default);

        public Task<PagedResult<TrackSummary>> GetTopTracksAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default);

        public Task<PagedResult<HistoryEntry>> GetHistoryAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default);

        // Retourne null si l'album n'existe pas dans le catalogue (indépendamment de l'historique d'écoute de l'utilisateur).
        public Task<AlbumDetail?> GetAlbumDetailAsync(Guid userId, Guid albumId, DateRange dateRange, CancellationToken ct = default);

        // Retourne null si l'artiste n'existe pas dans le catalogue (indépendamment de l'historique d'écoute de l'utilisateur).
        public Task<ArtistDetail?> GetArtistDetailAsync(Guid userId, Guid artistId, DateRange dateRange, CancellationToken ct = default);
    }
}
