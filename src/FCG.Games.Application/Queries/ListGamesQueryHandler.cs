using FCG.Games.Application.Abstractions;
using FCG.Games.Application.DTOs;
using FCG.Games.Application.Mappings;
using FCG.Games.Domain.Interfaces;
using FCG.Games.Domain.ValueObjects;

namespace FCG.Games.Application.Queries;

public sealed class ListGamesQueryHandler : IQueryHandler<ListGamesQuery, Result<IReadOnlyList<GameDto>>>
{
    private readonly IGameRepository _gameRepository;

    public ListGamesQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<IReadOnlyList<GameDto>>> HandleAsync(ListGamesQuery query, CancellationToken cancellationToken = default)
    {
        var games = await _gameRepository.GetAllAsync(cancellationToken);
        var dtos = games.Select(g => g.ToDto()).ToList();
        return Result.Success<IReadOnlyList<GameDto>>(dtos);
    }
}
