using DeezerStats.Domain.Aggregates.AlbumAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeezerStats.Infrastructure.Persistence.Configuration
{
    public class AlbumConfiguration : IEntityTypeConfiguration<Album>
    {
        public void Configure(EntityTypeBuilder<Album> builder)
        {
            builder.Property(a => a.NormalizedTitle)
                .HasMaxLength(255)
                .IsRequired();

            builder.HasAlternateKey(a => new { a.ArtistId, a.NormalizedTitle });

            // Conversion du Value Object Duration -> int (secondes)
            builder.Property(a => a.Duration)
                .HasConversion(DomainValueConverters.NullableDurationConverter);
        }
    }
}
