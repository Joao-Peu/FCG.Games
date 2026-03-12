using FCG.Games.Application.Abstractions;
using FCG.Games.Application.DTOs;
using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Enums;
using FCG.Games.Domain.Events;
using FCG.Games.Domain.Interfaces;
using FCG.Games.Domain.ValueObjects;

namespace FCG.Games.Application.Commands;

public sealed class PlaceOrderCommandHandler : ICommandHandler<PlaceOrderCommand, Result<PurchaseResultDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IUserLibraryRepository _userLibraryRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly string _orderPlacedTopic;

    public PlaceOrderCommandHandler(
        IGameRepository gameRepository,
        IUserLibraryRepository userLibraryRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        string orderPlacedTopic = "order-placed")
    {
        _gameRepository = gameRepository;
        _userLibraryRepository = userLibraryRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _orderPlacedTopic = orderPlacedTopic;
    }

    public async Task<Result<PurchaseResultDto>> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(command.GameId, cancellationToken);
        if (game is null)
            return Result.Failure<PurchaseResultDto>(Errors.GameNotFound);

        var alreadyOwned = await _userLibraryRepository.ExistsAsync(command.UserId, command.GameId, cancellationToken);
        if (alreadyOwned)
            return Result.Failure<PurchaseResultDto>(Errors.AlreadyOwned);

        var hasPending = await _orderRepository.HasPendingOrderAsync(command.UserId, command.GameId, cancellationToken);
        if (hasPending)
            return Result.Failure<PurchaseResultDto>(Errors.PendingOrder);

        var order = new OrderGame
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            GameId = command.GameId,
            Price = game.Price,
            Currency = game.Currency,
            Status = OrderStatus.PendingPayment,
            IsProcessed = false,
            RequestedAtUtc = DateTime.UtcNow,
            CorrelationId = command.CorrelationId
        };

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var @event = new OrderPlacedEvent(
            order.Id,
            order.UserId,
            order.GameId,
            order.Price,
            order.Currency,
            order.CorrelationId);

        await _eventPublisher.PublishAsync(_orderPlacedTopic, @event, cancellationToken);

        return Result.Success(new PurchaseResultDto(order.Id, nameof(OrderStatus.PendingPayment)));
    }
}
