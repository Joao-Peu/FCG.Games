namespace FCG.Games.Messaging;

public interface IPublisher
{
    Task PublishAsync<T>(string topic, T payload);
}
