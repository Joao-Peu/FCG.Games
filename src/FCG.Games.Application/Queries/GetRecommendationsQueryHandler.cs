using FCG.Games.Application.Abstractions;
using FCG.Games.Application.DTOs;
using FCG.Games.Application.Mappings;
using FCG.Games.Domain.Interfaces;
using FCG.Games.Domain.ValueObjects;

namespace FCG.Games.Application.Queries;

public sealed class GetRecommendationsQueryHandler : IQueryHandler<GetRecommendationsQuery, Result<IReadOnlyList<GameDto>>>
{
    private readonly IGameRepository _gameRepository;

    public GetRecommendationsQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<IReadOnlyList<GameDto>>> HandleAsync(GetRecommendationsQuery query, CancellationToken cancellationToken = default)
    {
        var games = await _gameRepository.GetTopByPriceAsync(10, cancellationToken);
        var dtos = games.Select(g => g.ToDto()).ToList();
        return Result.Success<IReadOnlyList<GameDto>>(dtos);
    }
}
