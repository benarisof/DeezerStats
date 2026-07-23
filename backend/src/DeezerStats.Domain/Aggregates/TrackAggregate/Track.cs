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

        /// <summary>
        /// Artistes en featuring sur ce morceau, tels qu'importés (ex. "Future" pour une ligne dont
        /// la colonne artiste valait "The Weeknd, Future"), stockés en texte libre pour l'affichage
        /// uniquement. Ne participe à aucune règle métier ni à aucun rattachement Artist/Album --
        /// seul le premier nom de la colonne artiste (voir ImportListeningHistoryUseCase) détermine
        /// l'artiste et l'album du morceau, afin d'éviter qu'un même album se retrouve fragmenté en
        /// plusieurs entités selon les featurings de chaque morceau.
        /// </summary>
        public string? FeaturedArtists { get; private set; }

        public Duration? Duration { get; private set; }

        public string? CoverUrl { get; private set; }

        /// <summary>
        /// Obtient une valeur indiquant si règle métier : Indique si le morceau possède déjà toutes les données enrichies par Deezer.
        /// </summary>
        public bool IsEnriched => Duration != null && !string.IsNullOrWhiteSpace(CoverUrl);

        /// <summary>
        /// Méthode d'enrichissement appelée lors du retour de l'API Deezer.
        /// </summary>
        /// <param name="duration">La durée du morceau récupérée depuis Deezer.</param>
        /// <param name="coverUrl">L'URL de la pochette du morceau récupérée depuis Deezer.</param>
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
