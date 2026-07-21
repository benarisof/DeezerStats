using DeezerStats.Domain.Aggregates.ListeningEventAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeezerStats.Infrastructure.Persistence.Configuration
{
    public class ListeningEventConfiguration : IEntityTypeConfiguration<ListeningEvent>
    {
        public void Configure(EntityTypeBuilder<ListeningEvent> builder)
        {
            // Pas de ToTable() explicite : convention EF Core par défaut (PascalCase pluralisé),
            // cohérente avec Albums/Artists/Users/Tracks et avec le nommage PascalCase de toutes
            // les colonnes.
            builder.HasKey(e => e.Id);

            builder.Property(e => e.UserId).IsRequired();
            builder.Property(e => e.TrackId).IsRequired();

            builder.Property(e => e.ListeningDuration)
                .HasConversion(DomainValueConverters.DurationConverter)
                .IsRequired();

            builder.Property(e => e.ListenedAt)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.HasOne<Track>()
                .WithMany()
                .HasForeignKey(e => e.TrackId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasAlternateKey(e => new { e.UserId, e.TrackId, e.ListenedAt });

            // Phase 9 (ticket 9.6) : les endpoints de consultation filtrent systématiquement par
            // utilisateur puis par plage de ListenedAt (historique, tops) ou groupent par
            // TrackId pour calculer les PlayCount (tops/pages item) — toujours scopés par
            // utilisateur. Ces deux index composites couvrent ces deux familles de requêtes sans
            // scanner la table entière.
            builder.HasIndex(e => new { e.UserId, e.ListenedAt });
            builder.HasIndex(e => new { e.UserId, e.TrackId });
        }
    }
}
