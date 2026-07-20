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
            Isrc isrc,
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
            Isrc = isrc ?? throw new ArgumentNullException(nameof(isrc));
            ListeningDuration = listeningDuration ?? throw new ArgumentNullException(nameof(listeningDuration));

            if (listenedAt > DateTime.UtcNow.AddMinutes(5))
            {
                throw new DomainException("La date d'écoute ne peut pas être dans le futur.");
            }

            ListenedAt = listenedAt;
        }

        private ListeningEvent()
        {
        }

        public Guid UserId { get; }

        public Guid TrackId { get; }

        public required Isrc Isrc { get; init; }

        public required Duration ListeningDuration { get; init; }

        public DateTime ListenedAt { get; }
    }
}
