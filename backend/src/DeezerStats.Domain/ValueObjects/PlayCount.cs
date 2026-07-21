using DeezerStats.Domain.SeedWork;

namespace DeezerStats.Domain.ValueObjects
{
    public record PlayCount
    {
        public PlayCount(int value = 0)
        {
            if (value < 0)
            {
                throw new DomainException("Le nombre d'écoutes ne peut pas être négatif.");
            }

            Value = value;
        }

        public int Value { get; init; }

        public PlayCount Increment() => new(Value + 1);
    }
}
