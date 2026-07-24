using DeezerStats.Domain.Aggregates.ArtistAggregate;

namespace DeezerStats.Application.Ports.Repositories
{
    /// <summary>
    /// Définit les opérations de persistance pour les artistes. AddAsync/AddRangeAsync/UpdateAsync
    /// ne déclenchent pas la persistance : l'appelant doit explicitement appeler
    /// <see cref="DeezerStats.Application.Ports.IUnitOfWork.SaveChangesAsync"/>, pour pouvoir
    /// committer plusieurs types d'entités (artistes, albums, morceaux, écoutes) en une seule
    /// transaction atomique.
    /// </summary>
    public interface IArtistRepository
    {
        public Task<Artist?> GetByIdAsync(Guid id, CancellationToken ct = default);

        // Comparaison insensible à la casse et aux espaces (nom normalisé), pour éviter les doublons à l'import.
        public Task<Artist?> GetByNameAsync(string name, CancellationToken ct = default);

        // Variante en lot de GetByNameAsync, pour éviter un aller-retour base par ligne lors des imports (~50 000 lignes).
        public Task<IReadOnlyList<Artist>> GetByNamesAsync(IEnumerable<string> names, CancellationToken ct = default);

        public Task AddAsync(Artist artist, CancellationToken ct = default);

        public Task AddRangeAsync(IEnumerable<Artist> artists, CancellationToken ct = default);

        public Task UpdateAsync(Artist artist, CancellationToken ct = default);
    }
}
