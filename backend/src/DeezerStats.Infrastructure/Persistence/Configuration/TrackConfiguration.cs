using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeezerStats.Infrastructure.Persistence.Configuration;

public class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        // Pas de ToTable() explicite : convention EF Core par défaut (PascalCase pluralisé),
        // cohérente avec Albums/Artists/Users et avec le nommage PascalCase de toutes les colonnes.
        builder.HasKey(t => t.Id);

        // Conversion du Value Object Isrc -> string avec type fixe
        builder.Property(t => t.Isrc)
            .HasConversion(DomainValueConverters.IsrcConverter)
            .HasColumnType("char(12)")
            .IsRequired();

        builder.HasIndex(t => t.Isrc).IsUnique();

        builder.Property(t => t.Title)
            .HasMaxLength(255)
            .IsRequired();

        // Conversion du Value Object Duration -> int (secondes)
        builder.Property(t => t.Duration)
            .HasConversion(DomainValueConverters.NullableDurationConverter);

        builder.Property(t => t.CoverUrl)
            .HasMaxLength(500);

        builder.HasOne<Artist>()
            .WithMany()
            .HasForeignKey(t => t.ArtistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Album>()
            .WithMany()
            .HasForeignKey(t => t.AlbumId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
