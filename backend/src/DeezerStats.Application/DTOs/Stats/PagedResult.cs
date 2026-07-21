namespace DeezerStats.Application.DTOs.Stats
{
    /// <summary>
    /// Enveloppe de pagination générique, réutilisée pour AlbumListPage/ArtistListPage/
    /// TrackListPage/HistoryPage (voir openapi.yaml) : ces quatre schémas ont exactement la même
    /// forme JSON (items/page/pageSize/totalItems/totalPages), seul le type des éléments diffère —
    /// inutile de dupliquer quatre types identiques.
    /// </summary>
    /// <typeparam name="T">Le type des éléments contenus dans la page (ex. <see cref="AlbumSummary"/>, <see cref="ArtistSummary"/>, <see cref="TrackSummary"/>, <see cref="HistoryEntry"/>).</typeparam>
    public record PagedResult<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        int TotalItems)
    {
        /// <summary>
        /// Obtient le nombre total de pages. Zéro si la liste est vide (plutôt qu'une page 0 ou 1 trompeuse).
        /// </summary>
        public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    }
}
