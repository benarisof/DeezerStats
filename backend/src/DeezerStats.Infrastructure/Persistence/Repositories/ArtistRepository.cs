using System;
using System.Collections.Generic;
using System.Text;
using DeezerStats.Application.Ports.Repositories;
using DeezerStats.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.Persistence.Repositories
{
    public class ArtistRepository(ApplicationDbContext context) : IArtistRepository
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<Artist?> GetByIdAsync(Guid id, CancellationToken ct = default) => await _context.Artists.FirstOrDefaultAsync(a => a.Id == id, ct);

        public async Task AddAsync(Artist artist, CancellationToken ct = default)
        {
            await _context.Artists.AddAsync(artist, ct);
            await _context.SaveChangesAsync(ct);
        }
    }
}
