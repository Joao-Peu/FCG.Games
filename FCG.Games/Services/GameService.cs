using Microsoft.EntityFrameworkCore;
using FCG.Games.Data;
using FCG.Games.Models;
using FCG.Games.Search;

namespace FCG.Games.Services;

public class GameService : IGameService
{
    private readonly AppDbContext _db;
    private readonly IGameSearchProvider _search;

    public GameService(AppDbContext db, IGameSearchProvider search)
    {
        _db = db;
        _search = search;
    }

    public async Task<Game> CreateAsync(Game game)
    {
        game.Id = Guid.NewGuid();
        game.CreatedAtUtc = DateTime.UtcNow;
        game.UpdatedAtUtc = DateTime.UtcNow;
        _db.Games.Add(game);
        await _db.SaveChangesAsync();
        return game;
    }

    public async Task<Game?> GetByIdAsync(Guid id)
    {
        return await _db.Games.FindAsync(id);
    }

    public async Task<(IEnumerable<Game> Items,int Total)> SearchAsync(string? query, string? genre, decimal? minPrice, decimal? maxPrice, int page, int pageSize)
    {
        return await _search.SearchAsync(query, genre, minPrice, maxPrice, page, pageSize);
    }

    public async Task<IEnumerable<Game>> GetRecommendationsAsync(Guid? userId)
    {
        // Simple recommendation: return top 10 most expensive as "popular" for demo purposes
        return await _db.Games.OrderByDescending(g => g.Price).Take(10).ToListAsync();
    }
}
