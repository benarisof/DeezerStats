namespace DeezerStats.Application.Common.Exceptions
{
    /// <summary>
    /// Représente un conflit métier : la ressource existe déjà et empêche l'opération demandée
    /// (ex. email déjà utilisé à l'inscription). Distincte de <see cref="DeezerStats.Domain.SeedWork.DomainException"/>
    /// (violation de règle de validation -> 400) pour permettre au middleware global de mapper ce
    /// cas vers un 409 Conflict, comme documenté dans le contrat OpenAPI (POST /auth/register).
    /// </summary>
    public class ConflictException : Exception
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
