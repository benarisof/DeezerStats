using DeezerStats.Domain.SeedWork;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Domain.Aggregates.TrackAggregate
{
    public class Track : Entity<Guid>, IAggregateRoot
    {
        public Track(Guid id, Isrc isrc, string title, Guid artistId, Guid albumId, string? featuredArtists = null)
            : base(id)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new DomainException("Le titre du morceau est obligatoire.");
            }

            Isrc = isrc ?? throw new ArgumentNullException(nameof(isrc));
            Title = title.Trim();
            ArtistId = artistId != Guid.Empty ? artistId : throw new DomainException("Un morceau doit être lié à un artiste.");
            AlbumId = albumId != Guid.Empty ? albumId : throw new DomainException("Un morceau doit être lié à un album.");
            FeaturedArtists = string.IsNullOrWhiteSpace(featuredArtists) ? null : featuredArtists.Trim();
        }

        private Track()
        {
        }

        public Isrc Isrc { get; private set; } = default!;

        public string Title { get; private set; } = default!;

        public Guid ArtistId { get; private set; }

        public Guid AlbumId { get; private set; }

        // Stocké en texte libre pour l'affichage uniquement (ex. "Future" pour "The Weeknd, Future") :
        // ne participe à aucun rattachement Artist/Album, seul le premier nom détermine ceux-ci (voir
        // ImportListeningHistoryUseCase), pour éviter qu'un même album se fragmente selon les featurings.
        public string? FeaturedArtists { get; private set; }

        public Duration? Duration { get; private set; }

        public string? CoverUrl { get; private set; }

        public bool IsEnriched => Duration != null && !string.IsNullOrWhiteSpace(CoverUrl);

        public void Enrich(Duration duration, string? coverUrl)
        {
            Duration = duration ?? throw new ArgumentNullException(nameof(duration));

            if (!string.IsNullOrWhiteSpace(coverUrl))
            {
                CoverUrl = coverUrl.Trim();
            }
        }
    }
}
