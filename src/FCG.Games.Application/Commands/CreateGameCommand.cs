namespace FCG.Games.Application.Commands;

public sealed record CreateGameCommand(
    string Title,
    string Description,
    decimal Price);
