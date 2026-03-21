using FCG.Games.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FCG.Games.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Games.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        var games = new List<Game>
        {
            new() { Id = Guid.NewGuid(), Title = "Cyber Odyssey", Description = "RPG futurista em mundo aberto com elementos cyberpunk", Price = 199.90m, CreatedAtUtc = now, UpdatedAtUtc = now },
            new() { Id = Guid.NewGuid(), Title = "Shadow Warriors", Description = "Ação furtiva em cenários medievais com combate tático", Price = 149.90m, CreatedAtUtc = now, UpdatedAtUtc = now },
            new() { Id = Guid.NewGuid(), Title = "Galactic Frontiers", Description = "Exploração espacial com construção de naves e diplomacia intergaláctica", Price = 179.90m, CreatedAtUtc = now, UpdatedAtUtc = now },
            new() { Id = Guid.NewGuid(), Title = "Dragon's Legacy", Description = "Aventura épica de fantasia com sistema de magia elemental", Price = 229.90m, CreatedAtUtc = now, UpdatedAtUtc = now },
            new() { Id = Guid.NewGuid(), Title = "Speed Revolution", Description = "Corrida arcade com pistas dinâmicas e personalização de veículos", Price = 99.90m, CreatedAtUtc = now, UpdatedAtUtc = now },
            new() { Id = Guid.NewGuid(), Title = "Zombie Survival", Description = "Sobrevivência em mundo pós-apocalíptico com crafting e base building", Price = 129.90m, CreatedAtUtc = now, UpdatedAtUtc = now },
            new() { Id = Guid.NewGuid(), Title = "Kingdom Builder", Description = "Estratégia em tempo real com gestão de recursos e batalhas épicas", Price = 159.90m, CreatedAtUtc = now, UpdatedAtUtc = now },
            new() { Id = Guid.NewGuid(), Title = "Mystic Puzzle", Description = "Quebra-cabeças misterioso com narrativa envolvente e mundos surreais", Price = 59.90m, CreatedAtUtc = now, UpdatedAtUtc = now },
            new() { Id = Guid.NewGuid(), Title = "Football Stars 2026", Description = "Simulação de futebol com modo carreira e multiplayer online", Price = 249.90m, CreatedAtUtc = now, UpdatedAtUtc = now },
            new() { Id = Guid.NewGuid(), Title = "Ocean Explorer", Description = "Aventura subaquática com exploração de oceanos e criaturas marinhas", Price = 139.90m, CreatedAtUtc = now, UpdatedAtUtc = now },
        };

        db.Games.AddRange(games);
        await db.SaveChangesAsync();

        logger.LogInformation("Seed: {Count} jogos criados no banco de dados", games.Count);
    }
}
