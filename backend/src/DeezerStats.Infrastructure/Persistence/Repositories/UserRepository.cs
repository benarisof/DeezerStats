using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Aggregates.UserAggregate;
using DeezerStats.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.Persistence.Repositories
{
    public class UserRepository(ApplicationDbContext context) : IUserRepository
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken ct = default)
            => await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id, ct);

        public async Task<User?> GetByEmailAsync(
            Email email,
            CancellationToken ct = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email, ct);
        }

        // Ne déclenche plus SaveChangesAsync elle-même (voir IUserRepository.AddAsync) : la
        // violation de la contrainte d'unicité sur l'email ne peut donc plus être détectée ici --
        // c'est désormais RegisterUserUseCase, au moment de son propre SaveChangesAsync, qui
        // retraduit un DbUpdateException en ConflictException.
        public async Task AddAsync(User user, CancellationToken ct = default) => await _context.Users.AddAsync(user, ct);
    }
}
