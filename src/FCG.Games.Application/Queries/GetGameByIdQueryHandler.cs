using FCG.Games.Application.Abstractions;
using FCG.Games.Application.DTOs;
using FCG.Games.Application.Mappings;
using FCG.Games.Domain.Interfaces;
using FCG.Games.Domain.ValueObjects;

namespace FCG.Games.Application.Queries;

public sealed class GetGameByIdQueryHandler : IQueryHandler<GetGameByIdQuery, Result<GameDto>>
{
    private readonly IGameRepository _gameRepository;

    public GetGameByIdQueryHandler(IGameRepository gameRepository) => _gameRepository = gameRepository;

    public async Task<Result<GameDto>> HandleAsync(GetGameByIdQuery query, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(query.Id, cancellationToken);
        if (game is null)
            return Result.Failure<GameDto>(Errors.GameNotFound);

        return Result.Success(game.ToDto());
    }
}
