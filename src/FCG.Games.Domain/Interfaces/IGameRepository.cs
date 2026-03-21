using FCG.Games.Domain.Entities;

namespace FCG.Games.Domain.Interfaces;

public interface IGameRepository
{
    Task<List<Game>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Game>> GetTopByPriceAsync(int count, CancellationToken cancellationToken = default);
    Task<List<Game>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Game game, CancellationToken cancellationToken = default);
    Task UpdateAsync(Game game, CancellationToken cancellationToken = default);
    Task DeleteAsync(Game game, CancellationToken cancellationToken = default);
}
