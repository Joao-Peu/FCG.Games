using FCG.Games.Models;

namespace FCG.Games.Search;

public class AzureSearchProvider : IGameSearchProvider
{
    private readonly string _endpoint;
    private readonly string? _key;

    public AzureSearchProvider(string endpoint, string? key)
    {
        _endpoint = endpoint;
        _key = key;
    }

    public Task<(IEnumerable<Game> Items,int Total)> SearchAsync(string? query, string? genre, decimal? minPrice, decimal? maxPrice, int page, int pageSize)
    {
        // Placeholder: in a real implementation use Azure.Search.Documents client
        // For now return empty with total 0 to allow wiring
        return Task.FromResult(((IEnumerable<Game>)Array.Empty<Game>(), 0));
    }
}
