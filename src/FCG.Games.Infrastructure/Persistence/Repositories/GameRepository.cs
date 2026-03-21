using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.Games.Infrastructure.Persistence.Repositories;

public class GameRepository : IGameRepository
{
    private readonly AppDbContext _context;

    public GameRepository(AppDbContext context) => _context = context;

    public async Task<List<Game>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Games.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Games.FindAsync(new object[] { id }, cancellationToken);

    public async Task<List<Game>> GetTopByPriceAsync(int count, CancellationToken cancellationToken = default) =>
        await _context.Games.AsNoTracking()
            .OrderByDescending(g => g.Price)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<List<Game>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
        await _context.Games.AsNoTracking()
            .Where(g => ids.Contains(g.Id))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Game game, CancellationToken cancellationToken = default) =>
        await _context.Games.AddAsync(game, cancellationToken);

    public Task UpdateAsync(Game game, CancellationToken cancellationToken = default)
    {
        _context.Games.Update(game);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Game game, CancellationToken cancellationToken = default)
    {
        _context.Games.Remove(game);
        return Task.CompletedTask;
    }
}
