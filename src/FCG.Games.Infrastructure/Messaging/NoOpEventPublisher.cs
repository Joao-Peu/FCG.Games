using FCG.Games.Domain.Interfaces;

namespace FCG.Games.Infrastructure.Messaging;

public class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
