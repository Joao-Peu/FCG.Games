namespace FCG.Games.Application.DTOs;

public sealed record GameDto(
    Guid Id,
    string Title,
    string Description,
    decimal Price);
