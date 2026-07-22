using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Aggregates.UserAggregate;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.Persistence.Repositories
{
    public class RefreshTokenRepository(ApplicationDbContext context) : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
            => await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default)
        {
            await _context.RefreshTokens.AddAsync(refreshToken, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct = default)
        {
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync(ct);
        }

        public async Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken ct = default)
        {
            List<RefreshToken> activeTokens = await _context.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ToListAsync(ct);

            foreach (RefreshToken token in activeTokens)
            {
                token.Revoke();
            }

            await _context.SaveChangesAsync(ct);
        }
    }
}
