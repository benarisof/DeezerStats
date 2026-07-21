using DeezerStats.Domain.SeedWork;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Domain.Aggregates.UserAggregate
{
    public class User : Entity<Guid>, IAggregateRoot
    {
        public User(
            Guid id,
            Email email,
            string passwordHash,
            string displayName)
            : base(id)
        {
            Email = email ?? throw new ArgumentNullException(nameof(email));

            ValidateDisplayName(displayName);

            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new DomainException(
                    "Le mot de passe haché ne peut pas être vide.");
            }

            PasswordHash = passwordHash;
            DisplayName = displayName.Trim();
            CreatedAt = DateTime.UtcNow;
        }

        private User()
        {
        }

        public Email Email { get; private set; } = default!;

        public string PasswordHash { get; private set; } = default!;

        public string DisplayName { get; private set; } = default!;

        public DateTime CreatedAt { get; private set; }

        public void UpdateProfile(string displayName)
        {
            ValidateDisplayName(displayName);

            DisplayName = displayName.Trim();
        }

        private static void ValidateDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new DomainException(
                    "Le nom d'affichage est obligatoire.");
            }
        }
    }
}
