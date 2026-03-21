using FCG.Games.Domain.Entities;

namespace FCG.Games.Domain.Interfaces;

public interface IUserLibraryRepository
{
    Task AddAsync(UserLibraryEntry entry, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetGameIdsByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
