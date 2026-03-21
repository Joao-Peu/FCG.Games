using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.Games.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(OrderGame order, CancellationToken cancellationToken = default) =>
        await _context.OrderGames.AddAsync(order, cancellationToken);

    public async Task<OrderGame?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.OrderGames.FindAsync(new object[] { id }, cancellationToken);

    public async Task<bool> HasPendingOrderAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default) =>
        await _context.OrderGames.AnyAsync(
            o => o.UserId == userId && o.GameId == gameId && !o.IsProcessed,
            cancellationToken);

    public Task UpdateAsync(OrderGame order, CancellationToken cancellationToken = default)
    {
        _context.OrderGames.Update(order);
        return Task.CompletedTask;
    }
}
