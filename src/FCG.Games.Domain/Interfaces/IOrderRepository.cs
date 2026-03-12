using FCG.Games.Domain.Entities;

namespace FCG.Games.Domain.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(OrderGame order, CancellationToken cancellationToken = default);
    Task<OrderGame?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasPendingOrderAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default);
    Task UpdateAsync(OrderGame order, CancellationToken cancellationToken = default);
}
