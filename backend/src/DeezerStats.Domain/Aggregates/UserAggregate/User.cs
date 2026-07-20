using DeezerStats.Domain.SeedWork;

namespace DeezerStats.Domain.Aggregates.UserAggregate
{
    public class User : Entity<Guid>, IAggregateRoot
    {
        public User(Guid id, string email, string passwordHash, string displayName)
            : base(id)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                throw new DomainException("L'adresse email est invalide.");
            }

            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new DomainException("Le mot de passe haché ne peut pas être vide.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new DomainException("Le nom d'affichage est obligatoire.");
            }

            Email = email.Trim().ToLowerInvariant();
            PasswordHash = passwordHash;
            DisplayName = displayName.Trim();
            CreatedAt = DateTime.UtcNow;
        }

        private User()
        {
        }

        public string Email { get; private set; } = default!;

        public string PasswordHash { get; private set; } = default!;

        public string DisplayName { get; private set; } = default!;

        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Permet la mise à jour du profil utilisateur.
        /// </summary>
        /// <param name="displayName">Le nouveau nom d'affichage de l'utilisateur.</param>
        public void UpdateProfile(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new DomainException("Le nom d'affichage ne peut pas être vide.");
            }

            DisplayName = displayName.Trim();
        }
    }
}
