namespace FCG.Games.Models;

public class AuditEvent
{
    public Guid EventId { get; set; }
    public string EntityName { get; set; } = null!;
    public string? EntityKey { get; set; }
    public string Action { get; set; } = null!;
    public string Data { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}
