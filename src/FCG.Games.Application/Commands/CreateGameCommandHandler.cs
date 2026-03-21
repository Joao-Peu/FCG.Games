using FCG.Games.Application.Abstractions;
using FCG.Games.Application.DTOs;
using FCG.Games.Application.Mappings;
using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Interfaces;
using FCG.Games.Domain.ValueObjects;

namespace FCG.Games.Application.Commands;

public sealed class CreateGameCommandHandler : ICommandHandler<CreateGameCommand, Result<GameDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGameCommandHandler(IGameRepository gameRepository, IUnitOfWork unitOfWork)
    {
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GameDto>> HandleAsync(CreateGameCommand command, CancellationToken cancellationToken = default)
    {
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Title = command.Title,
            Description = command.Description,
            Price = command.Price
        };

        await _gameRepository.AddAsync(game, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(game.ToDto());
    }
}
