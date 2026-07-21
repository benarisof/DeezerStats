using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.Queries
{
    /// <summary>
    /// Port de lecture pour les statistiques d'écoute (page d'accueil, tops, historique, pages
    /// item album/artiste). Distinct des repositories (Application.Ports.Repositories), qui
    /// restent orientés écriture/agrégat : ces requêtes traversent plusieurs agrégats
    /// (ListeningEvent -> Track -> Album/Artist) avec des agrégations (COUNT, GROUP BY) qui n'ont
    /// pas leur place dans un repository par agrégat.
    ///
    /// Toutes les méthodes sont scopées par utilisateur (voir ListeningEvent.UserId) : les
    /// statistiques d'un utilisateur ne doivent jamais fuiter vers un autre.
    /// </summary>
    public interface IListeningStatsQueryPort
    {
        /// <summary>
        /// Récupère les statistiques de la page d'accueil pour un utilisateur : top 10 des albums,
        /// artistes et morceaux les plus écoutés sur une période donnée.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur.</param>
        /// <param name="dateRange">Plage de dates à considérer pour les statistiques.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Un objet <see cref="HomeStatsResponse"/> contenant les tops 10 des albums, artistes et morceaux.</returns>
        public Task<HomeStatsResponse> GetHomeStatsAsync(Guid userId, DateRange dateRange, CancellationToken ct = default);

        /// <summary>
        /// Récupère le classement paginé des albums les plus écoutés pour un utilisateur sur une période donnée.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur.</param>
        /// <param name="dateRange">Plage de dates à considérer pour les statistiques.</param>
        /// <param name="page">Numéro de page (commence à 1).</param>
        /// <param name="pageSize">Nombre d'éléments par page.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Un résultat paginé contenant les résumés des albums classés par nombre d'écoutes décroissant.</returns>
        public Task<PagedResult<AlbumSummary>> GetTopAlbumsAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default);

        /// <summary>
        /// Récupère le classement paginé des artistes les plus écoutés pour un utilisateur sur une période donnée.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur.</param>
        /// <param name="dateRange">Plage de dates à considérer pour les statistiques.</param>
        /// <param name="page">Numéro de page (commence à 1).</param>
        /// <param name="pageSize">Nombre d'éléments par page.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Un résultat paginé contenant les résumés des artistes classés par nombre d'écoutes décroissant.</returns>
        public Task<PagedResult<ArtistSummary>> GetTopArtistsAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default);

        /// <summary>
        /// Récupère le classement paginé des morceaux les plus écoutés pour un utilisateur sur une période donnée.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur.</param>
        /// <param name="dateRange">Plage de dates à considérer pour les statistiques.</param>
        /// <param name="page">Numéro de page (commence à 1).</param>
        /// <param name="pageSize">Nombre d'éléments par page.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Un résultat paginé contenant les résumés des morceaux classés par nombre d'écoutes décroissant.</returns>
        public Task<PagedResult<TrackSummary>> GetTopTracksAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default);

        /// <summary>
        /// Récupère l'historique paginé des écoutes d'un utilisateur sur une période donnée, trié par date d'écoute décroissante.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur.</param>
        /// <param name="dateRange">Plage de dates à considérer pour l'historique.</param>
        /// <param name="page">Numéro de page (commence à 1).</param>
        /// <param name="pageSize">Nombre d'éléments par page.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Un résultat paginé contenant les entrées d'historique (morceau, album, artiste, date d'écoute).</returns>
        public Task<PagedResult<HistoryEntry>> GetHistoryAsync(Guid userId, DateRange dateRange, int page, int pageSize, CancellationToken ct = default);

        /// <summary>
        /// Récupère les détails d'un album pour un utilisateur sur une période donnée : agrégats (nombre total d'écoutes, durée totale, etc.)
        /// et la liste des morceaux de l'album triés par nombre d'écoutes décroissant.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur.</param>
        /// <param name="albumId">Identifiant de l'album.</param>
        /// <param name="dateRange">Plage de dates à considérer pour les statistiques.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Un objet <see cref="AlbumDetail"/> contenant les informations détaillées de l'album et ses morceaux classés, ou null si l'album n'existe pas pour cet utilisateur.</returns>
        public Task<AlbumDetail?> GetAlbumDetailAsync(Guid userId, Guid albumId, DateRange dateRange, CancellationToken ct = default);

        /// <summary>
        /// Récupère les détails d'un artiste pour un utilisateur sur une période donnée : agrégats (nombre total d'écoutes, durée totale, etc.)
        /// et la liste des morceaux de l'artiste triés par nombre d'écoutes décroissant.
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur.</param>
        /// <param name="artistId">Identifiant de l'artiste.</param>
        /// <param name="dateRange">Plage de dates à considérer pour les statistiques.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Un objet <see cref="ArtistDetail"/> contenant les informations détaillées de l'artiste et ses morceaux classés, ou null si l'artiste n'existe pas pour cet utilisateur.</returns>
        public Task<ArtistDetail?> GetArtistDetailAsync(Guid userId, Guid artistId, DateRange dateRange, CancellationToken ct = default);
    }
}
