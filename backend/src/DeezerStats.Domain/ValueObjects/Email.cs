using DeezerStats.Domain.SeedWork;

namespace DeezerStats.Domain.ValueObjects
{
    public record Email
    {
        public Email(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            {
                throw new DomainException("L'adresse email est invalide.");
            }

            Value = value.Trim().ToLowerInvariant();
        }

        public string Value { get; }
    }
}
