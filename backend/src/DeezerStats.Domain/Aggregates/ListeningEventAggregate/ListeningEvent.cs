using DeezerStats.Domain.SeedWork;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Domain.Aggregates.ListeningEventAggregate
{
    public class ListeningEvent : Entity<Guid>, IAggregateRoot
    {
        public ListeningEvent(
            Guid id,
            Guid userId,
            Guid trackId,
            Duration listeningDuration,
            DateTime listenedAt)
            : base(id)
        {
            if (userId == Guid.Empty)
            {
                throw new DomainException("L'événement d'écoute doit être rattaché à un utilisateur.");
            }

            if (trackId == Guid.Empty)
            {
                throw new DomainException("L'événement d'écoute doit être rattaché à un morceau.");
            }

            UserId = userId;
            TrackId = trackId;
            ListeningDuration = listeningDuration ?? throw new ArgumentNullException(nameof(listeningDuration));

            if (listenedAt > DateTime.UtcNow.AddMinutes(5))
            {
                throw new DomainException("La date d'écoute ne peut pas être dans le futur.");
            }

            ListenedAt = listenedAt;
        }

        private ListeningEvent()
        {
            ListeningDuration = null!;
        }

        public Guid UserId { get; }

        // TrackId est l'unique source de vérité pour identifier le morceau écouté : on ne duplique
        // plus l'Isrc ici (voir Track.Isrc). Une copie locale de l'Isrc n'était garantie d'aucune
        // synchronisation si l'ISRC d'un Track était un jour corrigé (ex. erreur d'import initiale
        // détectée après coup) — source de divergence silencieuse entre les deux tables.
        public Guid TrackId { get; }

        public Duration ListeningDuration { get; init; }

        public DateTime ListenedAt { get; }
    }
}
