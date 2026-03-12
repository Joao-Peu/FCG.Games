using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.Games.Infrastructure.Persistence.Repositories;

public class UserLibraryRepository : IUserLibraryRepository
{
    private readonly AppDbContext _context;

    public UserLibraryRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(UserLibraryEntry entry, CancellationToken cancellationToken = default) =>
        await _context.UserLibraryEntries.AddAsync(entry, cancellationToken);

    public async Task<bool> ExistsAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default) =>
        await _context.UserLibraryEntries.AnyAsync(
            e => e.UserId == userId && e.GameId == gameId,
            cancellationToken);
}
