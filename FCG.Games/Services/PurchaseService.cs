using FCG.Games.Data;
using FCG.Games.Models;
using FCG.Games.Messaging;

namespace FCG.Games.Services;

public class PurchaseService : IPurchaseService
{
    private readonly AppDbContext _db;
    private readonly IPublisher _publisher;

    public PurchaseService(AppDbContext db, IPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<PurchaseOrder> CreatePurchaseIntentAsync(Guid userId, Guid gameId, string? correlationId)
    {
        var game = await _db.Games.FindAsync(gameId) ?? throw new Exception("Game not found");
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GameId = gameId,
            Price = game.Price,
            Currency = game.Currency,
            Status = PurchaseStatus.PendingPayment,
            RequestedAtUtc = DateTime.UtcNow,
            CorrelationId = correlationId
        };
        _db.PurchaseOrders.Add(order);
        await _db.SaveChangesAsync();

        var evt = new PurchaseRequestedEvent
        {
            PurchaseId = order.Id,
            UserId = order.UserId,
            GameId = order.GameId,
            Price = order.Price,
            Currency = order.Currency,
            RequestedAtUtc = order.RequestedAtUtc,
            CorrelationId = correlationId
        };

        await _publisher.PublishAsync("purchase-requests", evt);

        return order;
    }
}
