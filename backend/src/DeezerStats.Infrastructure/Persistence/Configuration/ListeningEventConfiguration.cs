using DeezerStats.Domain.Aggregates.ListeningEventAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeezerStats.Infrastructure.Persistence.Configuration
{
    public class ListeningEventConfiguration : IEntityTypeConfiguration<ListeningEvent>
    {
        public void Configure(EntityTypeBuilder<ListeningEvent> builder)
        {
            builder.ToTable("listening_events");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.UserId).IsRequired();
            builder.Property(e => e.TrackId).IsRequired();

            builder.Property(e => e.ListeningDuration)
                .HasConversion(
                    d => d.TotalSeconds,
                    sec => new Duration(sec))
                .IsRequired();

            builder.Property(e => e.ListenedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.HasOne<Track>()
                .WithMany()
                .HasForeignKey(e => e.TrackId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasAlternateKey(e => new { e.UserId, e.TrackId, e.ListenedAt });
        }
    }
}
