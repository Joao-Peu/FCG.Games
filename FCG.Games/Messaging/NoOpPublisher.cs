namespace FCG.Games.Messaging;

public class NoOpPublisher : IPublisher
{
    public Task PublishAsync<T>(string topic, T payload)
    {
        // no-op for local development when Service Bus not configured
        return Task.CompletedTask;
    }
}
