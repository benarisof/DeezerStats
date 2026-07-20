using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.ListeningEventAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DeezerStats.Infrastructure.Persistence
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();

        public DbSet<Artist> Artists => Set<Artist>();

        public DbSet<Album> Albums => Set<Album>();

        public DbSet<Track> Tracks => Set<Track>();

        public DbSet<ListeningEvent> ListeningEvents => Set<ListeningEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Détection automatique de toutes les configurations de l'assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Convertisseur pour Duration non-nullable (ex: ListeningEvent)
            var durationConverterNonNullable = new ValueConverter<Duration, int>(
                d => d.TotalSeconds,
                s => new Duration(s));

            // Convertisseur pour Duration nullable (ex: Album, Track)
            var durationConverterNullable = new ValueConverter<Duration?, int?>(
                d => d == null ? (int?)null : d.TotalSeconds,
                s => s.HasValue ? new Duration(s.Value) : null);

            // Convertisseur pour Isrc (non-nullable dans les deux cas)
            var isrcConverter = new ValueConverter<Isrc, string>(
                i => i.Value,
                s => new Isrc(s));

            // Application des convertisseurs
            // Album.Duration est nullable
            modelBuilder.Entity<Album>()
                .Property(a => a.Duration)
                .HasConversion(durationConverterNullable);

            // Track.Duration est nullable
            modelBuilder.Entity<Track>()
                .Property(t => t.Duration)
                .HasConversion(durationConverterNullable);

            // ListeningEvent.ListeningDuration est non-nullable
            modelBuilder.Entity<ListeningEvent>()
                .Property(le => le.ListeningDuration)
                .HasConversion(durationConverterNonNullable);

            // Isrc est non-nullable dans Track et ListeningEvent
            modelBuilder.Entity<Track>()
                .Property(t => t.Isrc)
                .HasConversion(isrcConverter);

            modelBuilder.Entity<ListeningEvent>()
                .Property(le => le.Isrc)
                .HasConversion(isrcConverter);

            base.OnModelCreating(modelBuilder);
        }
    }
}
