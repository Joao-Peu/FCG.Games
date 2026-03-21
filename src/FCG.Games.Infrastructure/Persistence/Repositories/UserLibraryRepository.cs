using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.Games.Infrastructure.Persistence.Repositories;

public class UserLibraryRepository : IUserLibraryRepository
{
    private readonly AppDbContext _context;

    public UserLibraryRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(UserLibraryEntry entry, CancellationToken cancellationToken = default) =>
        await _context.UserLibraries.AddAsync(entry, cancellationToken);

    public async Task<bool> ExistsAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default) =>
        await _context.UserLibraries.AnyAsync(
            e => e.UserId == userId && e.GameId == gameId,
            cancellationToken);

    public async Task<List<Guid>> GetGameIdsByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _context.UserLibraries
            .Where(e => e.UserId == userId)
            .Select(e => e.GameId)
            .ToListAsync(cancellationToken);
}
