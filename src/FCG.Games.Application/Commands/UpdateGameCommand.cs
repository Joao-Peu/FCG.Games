namespace FCG.Games.Application.Commands;

public sealed record UpdateGameCommand(
    Guid Id,
    string Title,
    string Description,
    decimal Price);
