using System;
using System.Linq;
using System.Threading.Tasks;
using FCG.Games.Data;
using FCG.Games.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FCG.Games.Tests
{
    public class AppDbContextTests
    {
        [Fact]
        public async Task SaveChangesAsync_AddsAuditEntry_ForGame()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("auditdb")
                .Options;
            using var db = new AppDbContext(options);
            db.Games.Add(new Game { Id = Guid.NewGuid(), Title = "G" });
            await db.SaveChangesAsync();

            var audit = db.AuditEvents.FirstOrDefault();
            Assert.NotNull(audit);
            Assert.Equal("Game", audit.EntityName);
            Assert.Equal("Added", audit.Action);
        }
    }
}