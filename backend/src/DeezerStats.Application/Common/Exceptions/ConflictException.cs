using DeezerStats.Domain.SeedWork;

namespace DeezerStats.Application.Common.Exceptions
{
    /// <summary>
    /// Représente un conflit métier : la ressource existe déjà et empêche l'opération demandée
    /// (ex. email déjà utilisé à l'inscription). Hérite de <see cref="DomainException"/> — au lieu
    /// d'étendre directement <see cref="Exception"/> — pour que les exceptions applicatives forment
    /// une hiérarchie cohérente (voir aussi <see cref="AuthenticationFailedException"/>), plutôt que
    /// de mélanger DomainException et des types du framework selon les cas. Sémantiquement distincte
    /// d'une simple violation de règle (400) : le middleware la mappe vers un 409 Conflict, comme
    /// documenté dans le contrat OpenAPI (POST /auth/register).
    /// </summary>
    public class ConflictException : DomainException
    {
        public ConflictException(string message)
            : base(message)
        {
        }

        public ConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
