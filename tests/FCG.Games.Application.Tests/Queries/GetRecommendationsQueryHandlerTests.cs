using FCG.Games.Application.Queries;
using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace FCG.Games.Application.Tests.Queries;

public class GetRecommendationsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsTop10ByPrice()
    {
        var games = Enumerable.Range(1, 10)
            .Select(i => new Game
            {
                Id = Guid.NewGuid(),
                Title = $"Game {i}",
                Genre = "RPG",
                Price = i * 10m,
                Currency = "USD"
            })
            .ToList();

        var repo = Substitute.For<IGameRepository>();
        repo.GetTopByPriceAsync(10, Arg.Any<CancellationToken>()).Returns(games);

        var handler = new GetRecommendationsQueryHandler(repo);
        var result = await handler.HandleAsync(new GetRecommendationsQuery(null));

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value!.Count);
    }
}
