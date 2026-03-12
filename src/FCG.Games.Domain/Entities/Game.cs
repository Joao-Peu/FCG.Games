namespace FCG.Games.Domain.Entities;

public class Game
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Genre { get; set; } = null!;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
