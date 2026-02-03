using Microsoft.EntityFrameworkCore;
using FCG.Games.Data;
using FCG.Games.Models;

namespace FCG.Games.Search;

public class SqlSearchProvider : IGameSearchProvider
{
    private readonly AppDbContext _db;

    public SqlSearchProvider(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<Game> Items,int Total)> SearchAsync(string? query, string? genre, decimal? minPrice, decimal? maxPrice, int page, int pageSize)
    {
        var q = _db.Games.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            // simple full-text-like search on Title and Description
            q = q.Where(g => EF.Functions.Like(g.Title, $"%{query}%") || EF.Functions.Like(g.Description ?? string.Empty, $"%{query}%"));
        }
        if (!string.IsNullOrWhiteSpace(genre)) q = q.Where(g => g.Genre == genre);
        if (minPrice.HasValue) q = q.Where(g => g.Price >= minPrice.Value);
        if (maxPrice.HasValue) q = q.Where(g => g.Price <= maxPrice.Value);

        var total = await q.CountAsync();
        var items = await q.OrderBy(g => g.Title).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }
}
