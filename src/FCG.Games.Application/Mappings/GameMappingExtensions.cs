using FCG.Games.Application.DTOs;
using FCG.Games.Domain.Entities;

namespace FCG.Games.Application.Mappings;

public static class GameMappingExtensions
{
    public static GameDto ToDto(this Game game) =>
        new(game.Id, game.Title, game.Description, game.Price);
}
