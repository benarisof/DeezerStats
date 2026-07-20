using System.Text.RegularExpressions;
using DeezerStats.Domain.SeedWork;

namespace DeezerStats.Domain.ValueObjects
{
    public partial record Isrc
    {
        private static readonly Regex _isrcFormat = MyRegex();

        public Isrc(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainException("L'ISRC ne peut pas être vide ou invalide.");
            }

            if (!_isrcFormat.IsMatch(value))
            {
                throw new DomainException($"Le format de l'ISRC '{value}' est invalide.");
            }

            Value = value.ToUpperInvariant();
        }

        public string Value { get; init; }

        public override string ToString() => Value;

        [GeneratedRegex(@"^[A-Z]{2}[A-Z0-9]{3}\d{7}$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "fr-FR")]
        private static partial Regex MyRegex();
    }
}
