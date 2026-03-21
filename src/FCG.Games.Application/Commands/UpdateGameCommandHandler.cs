using FCG.Games.Application.Abstractions;
using FCG.Games.Application.DTOs;
using FCG.Games.Application.Mappings;
using FCG.Games.Domain.Interfaces;
using FCG.Games.Domain.ValueObjects;

namespace FCG.Games.Application.Commands;

public sealed class UpdateGameCommandHandler : ICommandHandler<UpdateGameCommand, Result<GameDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGameCommandHandler(IGameRepository gameRepository, IUnitOfWork unitOfWork)
    {
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GameDto>> HandleAsync(UpdateGameCommand command, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(command.Id, cancellationToken);
        if (game is null)
            return Result.Failure<GameDto>(Errors.GameNotFound);

        game.Title = command.Title;
        game.Description = command.Description;
        game.Price = command.Price;

        await _gameRepository.UpdateAsync(game, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(game.ToDto());
    }
}
