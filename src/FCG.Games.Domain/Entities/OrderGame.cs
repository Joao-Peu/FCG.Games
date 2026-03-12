using FCG.Games.Domain.Enums;

namespace FCG.Games.Domain.Entities;

public class OrderGame
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public bool IsProcessed { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
}
