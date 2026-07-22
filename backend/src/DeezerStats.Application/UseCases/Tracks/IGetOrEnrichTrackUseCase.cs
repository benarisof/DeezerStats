using DeezerStats.Domain.Aggregates.TrackAggregate;

namespace DeezerStats.Application.UseCases.Tracks
{
    public interface IGetOrEnrichTrackUseCase
    {
        /// <summary>
        /// Récupère ou enrichit un morceau à partir de sa requête (contenant l'ISRC et éventuellement d’autres informations).
        /// </summary>
        /// <param name="request">La requête contenant les informations pour identifier ou enrichir le morceau.</param>
        /// <param name="ct">Jeton d'annulation de l'opération.</param>
        /// <returns>Le morceau trouvé ou enrichi, ou <see langword="null" /> si aucun morceau correspondant n'a été trouvé.</returns>
        public Task<Track?> ExecuteAsync(GetOrEnrichTrackRequest request, CancellationToken ct = default);

        /// <summary>
        /// Variante recherchant le morceau par son identifiant plutôt que par son ISRC, utilisée
        /// quand l'appelant connaît déjà le morceau en base (ex. CatalogEnrichmentCoordinator, qui
        /// part des résultats déjà paginés d'un top morceaux).
        /// </summary>
        /// <param name="trackId">Identifiant du morceau.</param>
        /// <param name="ct">Jeton d'annulation pour l'opération asynchrone.</param>
        /// <returns>Le morceau correspondant à l'identifiant, ou <see langword="null" /> s'il n'existe pas.</returns>
        public Task<Track?> ExecuteByIdAsync(Guid trackId, CancellationToken ct = default);
    }
}
