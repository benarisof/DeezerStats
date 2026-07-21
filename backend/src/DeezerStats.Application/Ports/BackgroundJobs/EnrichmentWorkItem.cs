using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.BackgroundJobs
{
    /// <summary>
    /// Élément de travail planifié via <see cref="IEnrichmentJobScheduler"/> après un import (voir
    /// ImportListeningHistoryUseCase) et consommé en arrière-plan (voir
    /// Infrastructure.BackgroundJobs.EnrichmentBackgroundService) afin de ne jamais bloquer la
    /// réponse HTTP de POST /imports sur des appels réseau vers l'API Deezer.
    /// </summary>
    public abstract record EnrichmentWorkItem
    {
        /// <summary>
        /// Demande d'enrichissement d'un morceau, identifié par son ISRC (voir
        /// GetOrEnrichTrackUseCase).
        /// </summary>
        public sealed record ForTrack(Isrc Isrc) : EnrichmentWorkItem;

        /// <summary>
        /// Demande d'enrichissement d'un album, identifié par son identifiant (voir
        /// GetOrEnrichAlbumUseCase).
        /// </summary>
        public sealed record ForAlbum(Guid AlbumId) : EnrichmentWorkItem;

        /// <summary>
        /// Demande d'enrichissement d'un artiste, identifié par son identifiant (voir
        /// GetOrEnrichArtistUseCase).
        /// </summary>
        public sealed record ForArtist(Guid ArtistId) : EnrichmentWorkItem;
    }
}
