namespace FCG.Games.Messaging;

public class PurchaseRequestedEvent
{
    public Guid PurchaseId { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime RequestedAtUtc { get; set; }
    public string? CorrelationId { get; set; }
}
