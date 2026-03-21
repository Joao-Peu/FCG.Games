using FCG.Games.Application.Abstractions;
using FCG.Games.Domain.Interfaces;
using FCG.Games.Domain.ValueObjects;

namespace FCG.Games.Application.Commands;

public sealed class DeleteGameCommandHandler : ICommandHandler<DeleteGameCommand, Result>
{
    private readonly IGameRepository _gameRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteGameCommandHandler(IGameRepository gameRepository, IUnitOfWork unitOfWork)
    {
        _gameRepository = gameRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeleteGameCommand command, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(command.Id, cancellationToken);
        if (game is null)
            return Result.Failure(Errors.GameNotFound);

        await _gameRepository.DeleteAsync(game, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
