using System.Globalization;
using DeezerStats.Domain.SeedWork;

namespace DeezerStats.Domain.ValueObjects
{
    public record Duration
    {
        public Duration(int totalSeconds)
        {
            if (totalSeconds < 0)
            {
                throw new DomainException("Une durée ne peut pas être négative.");
            }

            TotalSeconds = totalSeconds;
        }

        public int TotalSeconds { get; init; }

        public override string ToString() => TimeSpan.FromSeconds(TotalSeconds).ToString(@"mm\:ss", CultureInfo.InvariantCulture);
    }
}
