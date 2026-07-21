using DeezerStats.Application.Common.Exceptions;
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

        public async Task AddAsync(
            User user,
            CancellationToken ct = default)
        {
            try
            {
                // Le conflit peut être détecté dès AddAsync (EF Core InMemory : identity map du
                // ChangeTracker, voir ArtistRepositoryTests pour le même comportement) ou seulement
                // au SaveChangesAsync (PostgreSQL réel : contrainte d'unicité en base) — les deux
                // appels sont donc couverts par le même bloc try/catch.
                await _context.Users.AddAsync(user, ct);
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is DbUpdateException or InvalidOperationException)
            {
                // Filet de sécurité pour la course concurrente (TOCTOU) que RegisterUserUseCase ne
                // peut pas éliminer à lui seul : deux inscriptions simultanées peuvent toutes deux
                // passer son contrôle GetByEmailAsync avant qu'aucune des deux ne soit committée.
                // La contrainte d'unicité en base (voir UserConfiguration.HasAlternateKey) rejette
                // alors la seconde. On la traduit ici en ConflictException pour que le contrat 409 de
                // l'API (voir openapi.yaml) reste vrai même dans ce cas limite, plutôt que de laisser
                // fuiter une erreur 500 générique.
                throw new ConflictException(
                    "Un utilisateur existe déjà avec cette adresse email.",
                    ex);
            }
        }
    }
}
