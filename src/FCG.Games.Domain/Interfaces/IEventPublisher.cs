namespace FCG.Games.Domain.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default);
}
