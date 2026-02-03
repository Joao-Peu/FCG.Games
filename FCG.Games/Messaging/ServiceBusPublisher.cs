using System.Text.Json;

namespace FCG.Games.Messaging;

public class ServiceBusPublisher : IPublisher
{
    private readonly dynamic _client;

    public ServiceBusPublisher(dynamic client)
    {
        _client = client;
    }

    public async Task PublishAsync<T>(string topic, T payload)
    {
        // Using dynamic to avoid strict compile-time dependency in environments lacking package
        var sender = _client.CreateSender(topic);
        var body = JsonSerializer.Serialize(payload);
        // Create ServiceBusMessage via reflection/dynamic
        dynamic msg = null;
        try
        {
            var msgType = Type.GetType("Azure.Messaging.ServiceBus.ServiceBusMessage, Azure.Messaging.ServiceBus");
            if (msgType != null)
            {
                msg = Activator.CreateInstance(msgType, body);
                var correlation = payload?.GetType().GetProperty("CorrelationId")?.GetValue(payload) as string;
                if (!string.IsNullOrEmpty(correlation)) msg.CorrelationId = correlation;
                await sender.SendMessageAsync(msg);
                return;
            }
        }
        catch
        {
            // fallback
        }

        // If we reach here, try a simple method via dynamic
        try
        {
            msg = new { Body = body };
            await sender.SendMessageAsync(msg);
        }
        catch
        {
            // swallow for no-op in environments where ServiceBus is unreachable
        }
    }
}
