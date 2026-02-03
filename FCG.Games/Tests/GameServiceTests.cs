using Xunit;
using Microsoft.EntityFrameworkCore;
using FCG.Games.Data;
using FCG.Games.Services;
using FCG.Games.Search;
using FCG.Games.Models;

namespace FCG.Games.Tests;

public class GameServiceTests
{
    [Fact]
    public async Task CreateGame_Persists()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "testdb")
            .Options;
        using var db = new AppDbContext(options);
        var search = new SqlSearchProvider(db);
        var svc = new GameService(db, search);

        var g = new Game { Title = "Test", Genre = "Action", Price = 9.99M };
        var created = await svc.CreateAsync(g);

        Assert.NotEqual(Guid.Empty, created.Id);
        var fetched = await svc.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
    }

    [Fact]
    public async Task Search_Returns_Paged()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "testdb2")
            .Options;
        using var db = new AppDbContext(options);
        db.Games.AddRange(new Game[]{ new Game{ Id=Guid.NewGuid(), Title="A Game", Genre="RPG", Price=5}, new Game{ Id=Guid.NewGuid(), Title="B Game", Genre="Action", Price=10}});
        await db.SaveChangesAsync();
        var search = new SqlSearchProvider(db);
        var svc = new GameService(db, search);

        var (items,total) = await svc.SearchAsync("Game", null, null, null, 1, 10);
        Assert.Equal(2, total);
    }
}
