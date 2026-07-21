using DeezerStats.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DeezerStats.Infrastructure.Persistence.Configuration
{
    /// <summary>
    /// Convertisseurs EF Core pour les Value Objects du domaine, partagés entre les
    /// <see cref="Microsoft.EntityFrameworkCore.IEntityTypeConfiguration{TEntity}"/> qui en ont
    /// besoin (voir TrackConfiguration, AlbumConfiguration, ListeningEventConfiguration,
    /// UserConfiguration). Centralisés ici pour n'avoir qu'un seul endroit à faire évoluer si la
    /// représentation en base d'un Value Object change — auparavant, certains de ces convertisseurs
    /// étaient redéfinis une seconde fois dans ApplicationDbContext.OnModelCreating, appliqué après
    /// les IEntityTypeConfiguration : la seconde définition écrasait silencieusement la première,
    /// rendant celle-ci trompeuse (code mort qui semblait pourtant faire foi).
    /// </summary>
    internal static class DomainValueConverters
    {
        public static readonly ValueConverter<Isrc, string> IsrcConverter =
            new(isrc => isrc.Value, value => new Isrc(value));

        public static readonly ValueConverter<Email, string> EmailConverter =
            new(email => email.Value, value => new Email(value));

        public static readonly ValueConverter<Duration, int> DurationConverter =
            new(duration => duration.TotalSeconds, seconds => new Duration(seconds));

        public static readonly ValueConverter<Duration?, int?> NullableDurationConverter = new(
            duration => duration == null ? null : duration.TotalSeconds,
            seconds => seconds.HasValue ? new Duration(seconds.Value) : null);
    }
}
