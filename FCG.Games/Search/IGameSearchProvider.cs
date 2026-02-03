using FCG.Games.Models;

namespace FCG.Games.Search;

public interface IGameSearchProvider
{
    Task<(IEnumerable<Game> Items,int Total)> SearchAsync(string? query, string? genre, decimal? minPrice, decimal? maxPrice, int page, int pageSize);
}
