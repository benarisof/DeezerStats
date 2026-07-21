using DeezerStats.Domain.SeedWork;

namespace DeezerStats.Application.Common.Exceptions
{
    /// <summary>
    /// Représente un échec d'authentification (email inconnu ou mot de passe invalide). Le message
    /// reste volontairement générique et identique dans les deux cas, pour ne pas laisser un
    /// attaquant déduire si un email existe en base. Hérite de <see cref="DomainException"/> plutôt
    /// que d'un type du framework comme <see cref="UnauthorizedAccessException"/>, afin que le
    /// middleware dispose d'une hiérarchie d'exceptions applicative cohérente pour son mapping HTTP
    /// (voir aussi <see cref="ConflictException"/>).
    /// </summary>
    public class AuthenticationFailedException : DomainException
    {
        public AuthenticationFailedException(string message)
            : base(message)
        {
        }

        public AuthenticationFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
