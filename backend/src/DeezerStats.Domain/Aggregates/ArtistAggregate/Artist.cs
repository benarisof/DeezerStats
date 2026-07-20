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
        }

        private Artist()
        {
        }

        public string Name { get; private set; } = default!;

        public string? CoverUrl { get; private set; }

        public void EnrichCover(string coverUrl)
        {
            if (!string.IsNullOrWhiteSpace(coverUrl))
            {
                CoverUrl = coverUrl.Trim();
            }
        }
    }
}
