using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeezerStats.Infrastructure.Persistence.Configuration;

public class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.ToTable("tracks");

        builder.HasKey(t => t.Id);

        // Conversion du Value Object Isrc -> string avec type fixe
        builder.Property(t => t.Isrc)
            .HasConversion(
                isrc => isrc.Value,
                value => new Isrc(value))
            .HasColumnType("char(12)")
            .IsRequired();

        builder.HasIndex(t => t.Isrc).IsUnique();

        builder.Property(t => t.Title)
            .HasMaxLength(255)
            .IsRequired();

        // Conversion du Value Object Duration -> int (secondes)
        builder.Property(t => t.Duration)
            .HasConversion(
                d => d != null ? d.TotalSeconds : (int?)null,
                sec => sec.HasValue ? new Duration(sec.Value) : null);

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
