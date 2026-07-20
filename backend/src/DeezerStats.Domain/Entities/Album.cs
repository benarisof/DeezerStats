using DeezerStats.Domain.SeedWork;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Domain.Entities
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
            ArtistId = artistId != Guid.Empty ? artistId : throw new DomainException("Un album doit être rattaché à un artiste.");
        }

        private Album()
        {
        }

        public string Title { get; private set; } = default!;

        public Guid ArtistId { get; private set; }

        public string? CoverUrl { get; private set; }

        public DateOnly? ReleaseDate { get; private set; }

        public Duration? Duration { get; private set; }

        public bool IsEnriched => !string.IsNullOrWhiteSpace(CoverUrl) && ReleaseDate.HasValue && Duration != null;

        /// <summary>
        /// Enrichit l'album avec les informations obtenues depuis l'API Deezer.
        /// </summary>
        /// <param name="coverUrl">L'URL de la pochette de l'album récupérée depuis Deezer.</param>
        /// <param name="releaseDate">La date de sortie de l'album récupérée depuis Deezer.</param>
        /// <param name="duration">La durée totale de l'album récupérée depuis Deezer.</param>
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
