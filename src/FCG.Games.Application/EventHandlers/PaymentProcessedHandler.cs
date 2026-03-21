using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Events;
using FCG.Games.Domain.Interfaces;

namespace FCG.Games.Application.EventHandlers;

public sealed class PaymentProcessedHandler : IEventHandler<PaymentProcessedEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserLibraryRepository _userLibraryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentProcessedHandler(
        IOrderRepository orderRepository,
        IUserLibraryRepository userLibraryRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _userLibraryRepository = userLibraryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(PaymentProcessedEvent @event, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(@event.OrderId, cancellationToken);
        if (order is null) return;

        order.IsProcessed = true;

        if (@event.Status == "Approved")
        {
            await _orderRepository.UpdateAsync(order, cancellationToken);

            var libraryEntry = new UserLibraryEntry
            {
                Id = Guid.NewGuid(),
                UserId = @event.UserId,
                GameId = @event.GameId,
                CreatedAt = DateTime.UtcNow
            };
            await _userLibraryRepository.AddAsync(libraryEntry, cancellationToken);
        }
        else
        {
            await _orderRepository.UpdateAsync(order, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
