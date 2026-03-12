using FCG.Games.Application.Commands;
using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Events;
using FCG.Games.Domain.Interfaces;
using FCG.Games.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace FCG.Games.Application.Tests.Commands;

public class PlaceOrderCommandHandlerTests
{
    private readonly IGameRepository _gameRepo = Substitute.For<IGameRepository>();
    private readonly IUserLibraryRepository _libraryRepo = Substitute.For<IUserLibraryRepository>();
    private readonly IOrderRepository _orderRepo = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly PlaceOrderCommandHandler _handler;

    public PlaceOrderCommandHandlerTests()
    {
        _handler = new PlaceOrderCommandHandler(
            _gameRepo,
            _libraryRepo,
            _orderRepo,
            _unitOfWork,
            _publisher,
            "order-placed");
    }

    [Fact]
    public async Task HandleAsync_HappyPath_CreatesOrderAndPublishesEvent()
    {
        var gameId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var game = new Game { Id = gameId, Title = "Test", Genre = "RPG", Price = 59.99m, Currency = "USD" };

        _gameRepo.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(game);
        _libraryRepo.ExistsAsync(userId, gameId, Arg.Any<CancellationToken>()).Returns(false);
        _orderRepo.HasPendingOrderAsync(userId, gameId, Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _handler.HandleAsync(new PlaceOrderCommand(userId, gameId, "corr-123"));

        Assert.True(result.IsSuccess);
        Assert.Equal("PendingPayment", result.Value!.Status);
        await _orderRepo.Received(1).AddAsync(Arg.Any<OrderGame>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync("order-placed", Arg.Any<OrderPlacedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_GameNotFound_ReturnsFailure()
    {
        var gameId = Guid.NewGuid();
        _gameRepo.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).Returns((Game?)null);

        var result = await _handler.HandleAsync(new PlaceOrderCommand(Guid.NewGuid(), gameId, null));

        Assert.True(result.IsFailure);
        Assert.Equal(Errors.GameNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_AlreadyOwned_ReturnsFailure()
    {
        var gameId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var game = new Game { Id = gameId, Title = "Test", Genre = "RPG", Price = 10m };

        _gameRepo.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(game);
        _libraryRepo.ExistsAsync(userId, gameId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.HandleAsync(new PlaceOrderCommand(userId, gameId, null));

        Assert.True(result.IsFailure);
        Assert.Equal(Errors.AlreadyOwned, result.Error);
    }

    [Fact]
    public async Task HandleAsync_PendingOrder_ReturnsFailure()
    {
        var gameId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var game = new Game { Id = gameId, Title = "Test", Genre = "RPG", Price = 10m };

        _gameRepo.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(game);
        _libraryRepo.ExistsAsync(userId, gameId, Arg.Any<CancellationToken>()).Returns(false);
        _orderRepo.HasPendingOrderAsync(userId, gameId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.HandleAsync(new PlaceOrderCommand(userId, gameId, null));

        Assert.True(result.IsFailure);
        Assert.Equal(Errors.PendingOrder, result.Error);
    }
}
