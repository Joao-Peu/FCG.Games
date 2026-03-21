namespace FCG.Games.Application.Commands;

public sealed record PlaceOrderCommand(Guid UserId, Guid GameId);
