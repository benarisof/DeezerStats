using DeezerStats.Domain.Aggregates.ArtistAggregate;
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

            builder.HasAlternateKey(a => a.NormalizedName);
        }
    }
}
