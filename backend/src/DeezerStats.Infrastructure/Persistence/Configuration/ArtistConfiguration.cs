using DeezerStats.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeezerStats.Infrastructure.Persistence.Configuration
{
    public class ArtistConfiguration : IEntityTypeConfiguration<Artist>
    {
        public void Configure(EntityTypeBuilder<Artist> builder)
        {
            builder.Property(a => a.NormalizedName)
                .HasMaxLength(255)
                .IsRequired();

            // Filet de sécurité en base : empêche la création de deux artistes de même nom (à la
            // casse/aux espaces près) même en cas de contournement de la recherche applicative
            // (ex. import concurrent).
            // NB : une clé alternative (plutôt qu'un simple index unique) est utilisée volontairement,
            // car c'est la seule forme d'unicité que le provider EF Core InMemory (utilisé dans les
            // tests, voir ArtistRepositoryTests) fait réellement respecter ; un HasIndex().IsUnique()
            // "nu" est ignoré par ce provider et ne l'aurait été qu'en base PostgreSQL réelle.
            builder.HasAlternateKey(a => a.NormalizedName);
        }
    }
}
