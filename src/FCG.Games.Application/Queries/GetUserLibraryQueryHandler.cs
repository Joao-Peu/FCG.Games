using FCG.Games.Application.Abstractions;
using FCG.Games.Application.DTOs;
using FCG.Games.Application.Mappings;
using FCG.Games.Domain.Interfaces;
using FCG.Games.Domain.ValueObjects;

namespace FCG.Games.Application.Queries;

public sealed class GetUserLibraryQueryHandler : IQueryHandler<GetUserLibraryQuery, Result<IReadOnlyList<GameDto>>>
{
    private readonly IUserLibraryRepository _userLibraryRepository;
    private readonly IGameRepository _gameRepository;

    public GetUserLibraryQueryHandler(
        IUserLibraryRepository userLibraryRepository,
        IGameRepository gameRepository)
    {
        _userLibraryRepository = userLibraryRepository;
        _gameRepository = gameRepository;
    }

    public async Task<Result<IReadOnlyList<GameDto>>> HandleAsync(GetUserLibraryQuery query, CancellationToken cancellationToken = default)
    {
        var gameIds = await _userLibraryRepository.GetGameIdsByUserAsync(query.UserId, cancellationToken);

        if (gameIds.Count == 0)
            return Result.Success<IReadOnlyList<GameDto>>(Array.Empty<GameDto>());

        var games = await _gameRepository.GetByIdsAsync(gameIds, cancellationToken);
        var dtos = games.Select(g => g.ToDto()).ToList();

        return Result.Success<IReadOnlyList<GameDto>>(dtos);
    }
}
