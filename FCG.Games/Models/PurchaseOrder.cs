namespace FCG.Games.Models;

public enum PurchaseStatus { PendingPayment, PaymentFailed, Completed }

public class PurchaseOrder
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public PurchaseStatus Status { get; set; } = PurchaseStatus.PendingPayment;
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
}
