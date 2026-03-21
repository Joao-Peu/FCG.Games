using FCG.Games.Application.Abstractions;
using FCG.Games.Application.DTOs;
using FCG.Games.Application.Mappings;
using FCG.Games.Domain.Interfaces;
using FCG.Games.Domain.ValueObjects;

namespace FCG.Games.Application.Queries;

public sealed class GetRecommendationsQueryHandler : IQueryHandler<GetRecommendationsQuery, Result<IReadOnlyList<GameDto>>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IUserLibraryRepository _userLibraryRepository;
    private const int MaxRecommendations = 10;

    public GetRecommendationsQueryHandler(
        IGameRepository gameRepository,
        IUserLibraryRepository userLibraryRepository)
    {
        _gameRepository = gameRepository;
        _userLibraryRepository = userLibraryRepository;
    }

    public async Task<Result<IReadOnlyList<GameDto>>> HandleAsync(GetRecommendationsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.UserId is null)
            return await FallbackTopGamesAsync(cancellationToken);

        var ownedGameIds = await _userLibraryRepository.GetGameIdsByUserAsync(query.UserId.Value, cancellationToken);
        if (ownedGameIds.Count == 0)
            return await FallbackTopGamesAsync(cancellationToken);

        var allGames = await _gameRepository.GetTopByPriceAsync(MaxRecommendations + ownedGameIds.Count, cancellationToken);
        var ownedSet = ownedGameIds.ToHashSet();
        var recommended = allGames
            .Where(g => !ownedSet.Contains(g.Id))
            .Take(MaxRecommendations)
            .Select(g => g.ToDto())
            .ToList();

        return Result.Success<IReadOnlyList<GameDto>>(recommended);
    }

    private async Task<Result<IReadOnlyList<GameDto>>> FallbackTopGamesAsync(CancellationToken cancellationToken)
    {
        var games = await _gameRepository.GetTopByPriceAsync(MaxRecommendations, cancellationToken);
        var dtos = games.Select(g => g.ToDto()).ToList();
        return Result.Success<IReadOnlyList<GameDto>>(dtos);
    }
}
