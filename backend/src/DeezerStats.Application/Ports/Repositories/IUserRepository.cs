using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.ValueObjects;

namespace DeezerStats.Application.Ports.Repositories
{
    public interface IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

        public Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);

        /// <summary>
        /// Ajoute un nouvel utilisateur au suivi du contexte SANS déclencher la persistance :
        /// l'appelant doit explicitement déclencher
        /// <see cref="DeezerStats.Application.Ports.IUnitOfWork.SaveChangesAsync"/> pour que cet
        /// utilisateur soit réellement écrit en base. C'est donc à cet appel (et non plus à
        /// AddAsync lui-même) qu'une violation de la contrainte d'unicité sur l'email peut
        /// remonter -- voir RegisterUserUseCase, qui la retraduit en ConflictException.
        /// </summary>
        /// <param name="user">Utilisateur à ajouter.</param>
        /// <param name="ct">Jeton d'annulation pour la requête asynchrone.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public Task AddAsync(User user, CancellationToken ct = default);
    }
}
