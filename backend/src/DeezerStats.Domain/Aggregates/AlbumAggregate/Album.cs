using DeezerStats.Domain.SeedWork;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Domain.Aggregates.AlbumAggregate
{
    public class Album : Entity<Guid>, IAggregateRoot
    {
        public Album(Guid id, string title, Guid artistId)
            : base(id)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new DomainException("Le titre de l'album est obligatoire.");
            }

            Title = title.Trim();
            NormalizedTitle = Normalize(Title);
            ArtistId = artistId != Guid.Empty ? artistId : throw new DomainException("Un album doit être rattaché à un artiste.");
        }

        private Album()
        {
        }

        public string Title { get; private set; } = default!;

        public string NormalizedTitle { get; private set; } = default!;

        public Guid ArtistId { get; private set; }

        public string? CoverUrl { get; private set; }

        public DateOnly? ReleaseDate { get; private set; }

        public Duration? Duration { get; private set; }

        public bool IsEnriched => !string.IsNullOrWhiteSpace(CoverUrl) && ReleaseDate.HasValue && Duration != null;

        public static string Normalize(string title) => title.Trim().ToLowerInvariant();

        public void Enrich(string? coverUrl, DateOnly? releaseDate, Duration? duration)
        {
            if (!string.IsNullOrWhiteSpace(coverUrl))
            {
                CoverUrl = coverUrl.Trim();
            }

            if (releaseDate.HasValue)
            {
                ReleaseDate = releaseDate.Value;
            }

            if (duration != null)
            {
                Duration = duration;
            }
        }
    }
}
