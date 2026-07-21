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

            // FK explicite vers Track : TrackId est l'unique référence au morceau écouté (l'ISRC
            // n'est plus dupliqué sur ListeningEvent, voir ListeningEvent.TrackId).
            builder.HasOne<Track>()
                .WithMany()
                .HasForeignKey(e => e.TrackId)
                .OnDelete(DeleteBehavior.Restrict);

            // Filet de sécurité en base : empêche la création de deux écoutes identiques (même
            // utilisateur, même morceau, même date d'écoute), même en cas de contournement de la
            // vérification applicative (voir ImportListeningHistoryUseCase). Une clé alternative
            // (plutôt qu'un simple index unique) est utilisée volontairement, car c'est la seule
            // forme d'unicité que le provider EF Core InMemory (utilisé dans les tests, voir
            // ListeningEventRepositoryTests) fait réellement respecter ; un HasIndex().IsUnique()
            // "nu" est ignoré par ce provider et ne l'aurait été qu'en base PostgreSQL réelle (même
            // choix que pour Artist/Album/User).
            builder.HasAlternateKey(e => new { e.UserId, e.TrackId, e.ListenedAt });
        }
    }
}
