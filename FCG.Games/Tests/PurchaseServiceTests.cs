using System;
using System.Threading.Tasks;
using FCG.Games.Data;
using FCG.Games.Messaging;
using FCG.Games.Models;
using FCG.Games.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FCG.Games.Tests
{
    public class PurchaseServiceTests
    {
        private class SpyPublisher : IPublisher
        {
            public object? LastMessage { get; private set; }
            public string? LastTopic { get; private set; }

            public Task PublishAsync(string topic, object message)
            {
                LastTopic = topic;
                LastMessage = message;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task CreatePurchaseIntent_CreatesOrder_AndPublishesEvent()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                        .UseInMemoryDatabase("purchasetest")
                        .Options;
            using var db = new AppDbContext(opts);
            var game = new Game { Id = Guid.NewGuid(), Title = "X", Genre = "Y", Price = 1.23M, Currency = "USD" };
            db.Games.Add(game);
            await db.SaveChangesAsync();

            var spy = new SpyPublisher();
            var svc = new PurchaseService(db, spy);

            var userId = Guid.NewGuid();
            var order = await svc.CreatePurchaseIntentAsync(userId, game.Id, "corr-1");

            Assert.Equal(userId, order.UserId);
            Assert.Equal(game.Id, order.GameId);
            Assert.Equal(PurchaseStatus.PendingPayment, order.Status);
            Assert.Equal("corr-1", order.CorrelationId);
            Assert.NotEqual(Guid.Empty, order.Id);

            Assert.Equal("purchase-requests", spy.LastTopic);
            Assert.NotNull(spy.LastMessage);
            Assert.IsType<PurchaseRequestedEvent>(spy.LastMessage);
        }
    }
}