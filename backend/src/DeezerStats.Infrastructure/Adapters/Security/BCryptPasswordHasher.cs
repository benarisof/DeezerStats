using DeezerStats.Application.Ports.Security;

namespace DeezerStats.Infrastructure.Adapters.Security
{
    public class BCryptPasswordHasher : IPasswordHasher
    {
        private const int _workFactor = 12;

        public string Hash(string plainTextPassword)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword))
            {
                throw new ArgumentException("Le mot de passe ne peut pas être vide.", nameof(plainTextPassword));
            }

            return BCrypt.Net.BCrypt.HashPassword(plainTextPassword, _workFactor);
        }

        public bool Verify(string plainTextPassword, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(plainTextPassword) || string.IsNullOrWhiteSpace(hashedPassword))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
