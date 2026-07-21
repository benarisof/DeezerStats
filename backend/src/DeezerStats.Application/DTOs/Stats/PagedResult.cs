namespace DeezerStats.Application.DTOs.Stats
{
    /// <summary>
    /// Enveloppe de pagination générique, réutilisée pour AlbumListPage/ArtistListPage/
    /// TrackListPage/HistoryPage (voir openapi.yaml) : ces quatre schémas ont exactement la même
    /// forme JSON (items/page/pageSize/totalItems/totalPages), seul le type des éléments diffère —
    /// inutile de dupliquer quatre types identiques.
    /// </summary>
    public record PagedResult<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        int TotalItems)
    {
        /// <summary>
        /// Nombre total de pages. Zéro si la liste est vide (plutôt qu'une page 0 ou 1 trompeuse).
        /// </summary>
        public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    }
}
