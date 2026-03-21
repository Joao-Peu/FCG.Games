using FCG.Games.Application.Queries;
using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace FCG.Games.Application.Tests.Queries;

public class ListGamesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsMappedGames()
    {
        var games = new List<Game>
        {
            new() { Id = Guid.NewGuid(), Title = "Game 1", Description = "Desc 1", Price = 29.99m },
            new() { Id = Guid.NewGuid(), Title = "Game 2", Description = "Desc 2", Price = 49.99m }
        };

        var repo = Substitute.For<IGameRepository>();
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(games);

        var handler = new ListGamesQueryHandler(repo);
        var result = await handler.HandleAsync(new ListGamesQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal("Game 1", result.Value[0].Title);
        Assert.Equal("Game 2", result.Value[1].Title);
    }
}
