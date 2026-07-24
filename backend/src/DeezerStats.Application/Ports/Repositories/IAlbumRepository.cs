using DeezerStats.Domain.Aggregates.AlbumAggregate;

namespace DeezerStats.Application.Ports.Repositories
{
    /// <summary>
    /// Définit les opérations de persistance pour les albums. AddAsync/AddRangeAsync/UpdateAsync ne
    /// déclenchent pas la persistance : l'appelant doit explicitement appeler
    /// <see cref="DeezerStats.Application.Ports.IUnitOfWork.SaveChangesAsync"/>, pour pouvoir
    /// committer plusieurs types d'entités (artistes, albums, morceaux, écoutes) en une seule
    /// transaction atomique.
    /// </summary>
    public interface IAlbumRepository
    {
        public Task<Album?> GetByIdAsync(Guid id, CancellationToken ct = default);

        // Comparaison insensible à la casse et aux espaces (titre normalisé), pour éviter les doublons à l'import.
        public Task<Album?> GetByTitleAndArtistAsync(string title, Guid artistId, CancellationToken ct = default);

        // Variante en lot de GetByTitleAndArtistAsync, pour éviter un aller-retour base par ligne lors des imports (~50 000 lignes).
        public Task<IReadOnlyList<Album>> GetByArtistIdsAsync(IEnumerable<Guid> artistIds, CancellationToken ct = default);

        public Task AddAsync(Album album, CancellationToken ct = default);

        public Task AddRangeAsync(IEnumerable<Album> albums, CancellationToken ct = default);

        public Task UpdateAsync(Album album, CancellationToken ct = default);
    }
}
