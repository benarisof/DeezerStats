using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.Entities;
using DeezerStats.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DeezerStats.Infrastructure.Persistence
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();

        public DbSet<Artist> Artists => Set<Artist>();

        public DbSet<Album> Albums => Set<Album>();

        public DbSet<Track> Tracks => Set<Track>();

        public DbSet<ListeningEvent> ListeningEvents => Set<ListeningEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Détection automatique de toutes les configurations de l'assembly
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(ApplicationDbContext).Assembly);

            // Convertisseur pour Duration non-nullable
            var durationConverterNonNullable = new ValueConverter<Duration, int>(
                d => d.TotalSeconds,
                s => new Duration(s));

            // Convertisseur pour Duration nullable
            var durationConverterNullable = new ValueConverter<Duration?, int?>(
                d => d == null ? null : d.TotalSeconds,
                s => s.HasValue ? new Duration(s.Value) : null);

            // Convertisseur pour Isrc
            var isrcConverter = new ValueConverter<Isrc, string>(
                i => i.Value,
                s => new Isrc(s));

            // Convertisseur pour Email
            var emailConverter = new ValueConverter<Email, string>(
                e => e.Value,
                s => new Email(s));

            // Album.Duration
            modelBuilder.Entity<Album>()
                .Property(a => a.Duration)
                .HasConversion(durationConverterNullable);

            // Track.Duration
            modelBuilder.Entity<Track>()
                .Property(t => t.Duration)
                .HasConversion(durationConverterNullable);

            // ListeningEvent.ListeningDuration
            modelBuilder.Entity<ListeningEvent>()
                .Property(le => le.ListeningDuration)
                .HasConversion(durationConverterNonNullable);

            // Track.Isrc
            modelBuilder.Entity<Track>()
                .Property(t => t.Isrc)
                .HasConversion(isrcConverter);

            // ListeningEvent.Isrc
            modelBuilder.Entity<ListeningEvent>()
                .Property(le => le.Isrc)
                .HasConversion(isrcConverter);

            // User.Email
            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasConversion(emailConverter);

            base.OnModelCreating(modelBuilder);
        }
    }
}
