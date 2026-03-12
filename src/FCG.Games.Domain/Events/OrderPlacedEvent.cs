namespace FCG.Games.Domain.Events;

public sealed record OrderPlacedEvent(
    Guid OrderId,
    Guid UserId,
    Guid GameId,
    decimal Price,
    string Currency,
    string? CorrelationId);
