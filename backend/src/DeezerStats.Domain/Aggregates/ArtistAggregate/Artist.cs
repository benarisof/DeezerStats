using DeezerStats.Domain.SeedWork;

namespace DeezerStats.Domain.Aggregates.ArtistAggregate
{
    /// <summary>
    /// Représente un artiste musical.
    /// </summary>
    public class Artist : Entity<Guid>, IAggregateRoot
    {
        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="Artist"/>.
        /// </summary>
        /// <param name="id">Identifiant unique de l'artiste.</param>
        /// <param name="name">Nom de l'artiste (ne peut pas être vide ou composé uniquement d'espaces).</param>
        /// <exception cref="DomainException">Levée si <paramref name="name"/> est vide ou null.</exception>
        public Artist(Guid id, string name)
            : base(id)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new DomainException("Le nom de l'artiste est obligatoire.");
            }

            Name = name.Trim();
            NormalizedName = Normalize(Name);
        }

        // Constructeur privé pour Entity Framework (ne pas supprimer)
        private Artist()
        {
        }

        /// <summary>
        /// Obtient le nom affiché de l'artiste (tel que saisi).
        /// </summary>
        public string Name { get; private set; } = default!;

        /// <summary>
        /// Obtient la version normalisée du nom (minuscules, espaces superflus supprimés),
        /// utilisée pour la recherche et la contrainte d'unicité en base.
        /// </summary>
        public string NormalizedName { get; private set; } = default!;

        /// <summary>
        /// Obtient l'URL de la couverture (pochette) de l'artiste.
        /// </summary>
        public string? CoverUrl { get; private set; }

        /// <summary>
        /// Normalise un nom d'artiste pour la comparaison/recherche (insensible à la casse et aux
        /// espaces superflus en début/fin de chaîne).
        /// </summary>
        /// <param name="name">Nom de l'artiste à normaliser.</param>
        /// <returns>Le nom normalisé (minuscules, sans espaces superflus).</returns>
        public static string Normalize(string name) => name.Trim().ToLowerInvariant();

        /// <summary>
        /// Met à jour l'URL de couverture de l'artiste.
        /// </summary>
        /// <param name="coverUrl">Nouvelle URL de la couverture.</param>
        public void EnrichCover(string coverUrl)
        {
            if (!string.IsNullOrWhiteSpace(coverUrl))
            {
                CoverUrl = coverUrl.Trim();
            }
        }
    }
}
