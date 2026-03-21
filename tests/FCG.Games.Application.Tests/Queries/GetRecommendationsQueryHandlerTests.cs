using FCG.Games.Application.Queries;
using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace FCG.Games.Application.Tests.Queries;

public class GetRecommendationsQueryHandlerTests
{
    private readonly IGameRepository _gameRepo = Substitute.For<IGameRepository>();
    private readonly IUserLibraryRepository _libraryRepo = Substitute.For<IUserLibraryRepository>();

    private GetRecommendationsQueryHandler CreateHandler() =>
        new(_gameRepo, _libraryRepo);

    [Fact]
    public async Task HandleAsync_NoUserId_ReturnsFallbackTopGames()
    {
        var games = CreateGames(10);
        _gameRepo.GetTopByPriceAsync(10, Arg.Any<CancellationToken>()).Returns(games);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new GetRecommendationsQuery(null));

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value!.Count);
    }

    [Fact]
    public async Task HandleAsync_UserWithNoLibrary_ReturnsFallbackTopGames()
    {
        var userId = Guid.NewGuid();
        var games = CreateGames(10);
        _libraryRepo.GetGameIdsByUserAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<Guid>());
        _gameRepo.GetTopByPriceAsync(10, Arg.Any<CancellationToken>()).Returns(games);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new GetRecommendationsQuery(userId));

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value!.Count);
    }

    [Fact]
    public async Task HandleAsync_UserWithLibrary_ExcludesOwnedGames()
    {
        var userId = Guid.NewGuid();
        var ownedGameId = Guid.NewGuid();

        _libraryRepo.GetGameIdsByUserAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<Guid> { ownedGameId });

        var allGames = new List<Game>
        {
            new() { Id = ownedGameId, Title = "Owned", Description = "Owned game", Price = 50m },
            new() { Id = Guid.NewGuid(), Title = "Game 1", Description = "Desc 1", Price = 40m },
            new() { Id = Guid.NewGuid(), Title = "Game 2", Description = "Desc 2", Price = 30m }
        };
        _gameRepo.GetTopByPriceAsync(11, Arg.Any<CancellationToken>()).Returns(allGames);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new GetRecommendationsQuery(userId));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.DoesNotContain(result.Value, g => g.Id == ownedGameId);
    }

    private static List<Game> CreateGames(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new Game
            {
                Id = Guid.NewGuid(),
                Title = $"Game {i}",
                Description = $"Description {i}",
                Price = i * 10m
            })
            .ToList();
}
