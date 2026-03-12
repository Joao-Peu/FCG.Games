using FCG.Games.Application.EventHandlers;
using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Enums;
using FCG.Games.Domain.Events;
using FCG.Games.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace FCG.Games.Application.Tests.EventHandlers;

public class PaymentProcessedHandlerTests
{
    private readonly IOrderRepository _orderRepo = Substitute.For<IOrderRepository>();
    private readonly IUserLibraryRepository _libraryRepo = Substitute.For<IUserLibraryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PaymentProcessedHandler _handler;

    public PaymentProcessedHandlerTests()
    {
        _handler = new PaymentProcessedHandler(_orderRepo, _libraryRepo, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_Approved_CompletesOrderAndAddsToLibrary()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var order = new OrderGame
        {
            Id = orderId,
            UserId = userId,
            GameId = gameId,
            Price = 59.99m,
            Currency = "USD",
            Status = OrderStatus.PendingPayment,
            IsProcessed = false
        };

        _orderRepo.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var @event = new PaymentProcessedEvent(orderId, userId, gameId, 59.99m, "Approved");
        await _handler.HandleAsync(@event);

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.True(order.IsProcessed);
        await _libraryRepo.Received(1).AddAsync(
            Arg.Is<UserLibraryEntry>(e => e.UserId == userId && e.GameId == gameId),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Rejected_SetsPaymentFailed()
    {
        var orderId = Guid.NewGuid();
        var order = new OrderGame
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            Price = 59.99m,
            Status = OrderStatus.PendingPayment,
            IsProcessed = false
        };

        _orderRepo.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var @event = new PaymentProcessedEvent(orderId, Guid.NewGuid(), Guid.NewGuid(), 59.99m, "Rejected");
        await _handler.HandleAsync(@event);

        Assert.Equal(OrderStatus.PaymentFailed, order.Status);
        Assert.False(order.IsProcessed);
        await _libraryRepo.DidNotReceive().AddAsync(Arg.Any<UserLibraryEntry>(), Arg.Any<CancellationToken>());
    }
}
