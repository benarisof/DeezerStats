using DeezerStats.Domain.SeedWork;

namespace DeezerStats.Domain.Aggregates.ArtistAggregate
{
    public class Artist : Entity<Guid>, IAggregateRoot
    {
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

        public string Name { get; private set; } = default!;

        public string NormalizedName { get; private set; } = default!;

        public string? CoverUrl { get; private set; }

        // Seule la couverture est enrichissable pour un artiste (contrairement à un morceau ou un
        // album, voir Track.IsEnriched/Album.IsEnriched).
        public bool IsEnriched => !string.IsNullOrWhiteSpace(CoverUrl);

        public static string Normalize(string name) => name.Trim().ToLowerInvariant();

        public void EnrichCover(string? coverUrl)
        {
            if (!string.IsNullOrWhiteSpace(coverUrl))
            {
                CoverUrl = coverUrl.Trim();
            }
        }
    }
}
