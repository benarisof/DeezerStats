using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.ListeningEventAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.Aggregates.UserAggregate;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.Persistence
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<Artist> Artists => Set<Artist>();

        public DbSet<Album> Albums => Set<Album>();

        public DbSet<Track> Tracks => Set<Track>();

        public DbSet<ListeningEvent> ListeningEvents => Set<ListeningEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Toute la configuration du modèle (y compris les convertisseurs de Value Objects, voir
            // Configuration/DomainValueConverters) vit dans les IEntityTypeConfiguration<T> de
            // Persistence/Configuration — aucune configuration ad hoc ici, pour n'avoir qu'un seul
            // endroit faisant foi par agrégat.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
