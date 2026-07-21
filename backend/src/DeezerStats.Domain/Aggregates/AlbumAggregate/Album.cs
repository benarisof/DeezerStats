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

        /// <summary>
        /// Obtient version normalisée du titre (minuscules, espaces superflus supprimés), utilisée pour la
        /// recherche et la contrainte d'unicité en base (par couple avec l'artiste). Permet d'éviter
        /// la création de doublons d'album lors de l'import.
        /// </summary>
        public string NormalizedTitle { get; private set; } = default!;

        public Guid ArtistId { get; private set; }

        public string? CoverUrl { get; private set; }

        public DateOnly? ReleaseDate { get; private set; }

        public Duration? Duration { get; private set; }

        public bool IsEnriched => !string.IsNullOrWhiteSpace(CoverUrl) && ReleaseDate.HasValue && Duration != null;

        /// <summary>
        /// Normalise un titre d'album pour la comparaison/recherche (insensible à la casse et aux
        /// espaces superflus en début/fin de chaîne).
        /// </summary>
        /// <param name="title">Titre de l'album à normaliser.</param>
        /// <returns>Le titre normalisé (minuscules, sans espaces superflus).</returns>
        public static string Normalize(string title) => title.Trim().ToLowerInvariant();

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
