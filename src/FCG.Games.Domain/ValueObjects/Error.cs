namespace FCG.Games.Domain.ValueObjects;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}

public static class Errors
{
    public static readonly Error GameNotFound = new("Game.NotFound", "Game not found.");
    public static readonly Error AlreadyOwned = new("Game.AlreadyOwned", "User already owns this game.");
    public static readonly Error PendingOrder = new("Order.Pending", "User already has a pending order for this game.");
}
