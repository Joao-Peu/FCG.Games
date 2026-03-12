using FCG.Games.Domain.Entities;
using FCG.Games.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FCG.Games.Infrastructure.Tests.Persistence;

public class AppDbContextTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SaveChangesAsync_AddsAuditEntry_ForNewGame()
    {
        using var context = CreateInMemoryContext();

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Title = "Test Game",
            Genre = "RPG",
            Price = 29.99m,
            Currency = "USD"
        };

        context.Games.Add(game);
        await context.SaveChangesAsync();

        var audits = await context.AuditEvents.ToListAsync();
        Assert.Single(audits);
        Assert.Equal("Game", audits[0].EntityName);
        Assert.Equal("Added", audits[0].Action);
        Assert.Equal(game.Id.ToString(), audits[0].EntityKey);
    }

    [Fact]
    public async Task SaveChangesAsync_AddsAuditEntry_ForModifiedGame()
    {
        using var context = CreateInMemoryContext();

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Title = "Original",
            Genre = "RPG",
            Price = 10m
        };

        context.Games.Add(game);
        await context.SaveChangesAsync();

        game.Title = "Updated";
        context.Games.Update(game);
        await context.SaveChangesAsync();

        var audits = await context.AuditEvents.ToListAsync();
        Assert.Equal(2, audits.Count);
        Assert.Contains(audits, a => a.Action == "Added");
        Assert.Contains(audits, a => a.Action == "Modified");
    }

    [Fact]
    public async Task SaveChangesAsync_DoesNotAudit_AuditEventItself()
    {
        using var context = CreateInMemoryContext();

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            Genre = "FPS",
            Price = 5m
        };

        context.Games.Add(game);
        await context.SaveChangesAsync();

        // Only one audit for the Game, not for the AuditEvent itself
        var audits = await context.AuditEvents.ToListAsync();
        Assert.Single(audits);
        Assert.Equal("Game", audits[0].EntityName);
    }
}
