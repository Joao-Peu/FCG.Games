using FCG.Games.Models;

namespace FCG.Games.Services;

public interface IGameService
{
    Task<Game> CreateAsync(Game game);
    Task<Game?> GetByIdAsync(Guid id);
    Task<(IEnumerable<Game> Items,int Total)> SearchAsync(string? query, string? genre, decimal? minPrice, decimal? maxPrice, int page, int pageSize);
    Task<IEnumerable<Game>> GetRecommendationsAsync(Guid? userId);
}
