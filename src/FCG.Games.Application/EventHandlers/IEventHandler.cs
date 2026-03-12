namespace FCG.Games.Application.EventHandlers;

public interface IEventHandler<in T>
{
    Task HandleAsync(T @event, CancellationToken cancellationToken = default);
}
