using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FCG.Games.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FCG.Games.Infrastructure.Messaging;

public class ServiceBusEventPublisher : IEventPublisher
{
    private static readonly Meter s_meter = new("FCG.Games");
    private static readonly Counter<long> s_published = s_meter.CreateCounter<long>(
        "fcg.servicebus.messages_published", description: "Mensagens publicadas no Service Bus");
    private static readonly Counter<long> s_errors = s_meter.CreateCounter<long>(
        "fcg.servicebus.publish_errors", description: "Erros ao publicar no Service Bus");

    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusEventPublisher> _logger;

    public ServiceBusEventPublisher(ServiceBusClient client, ILogger<ServiceBusEventPublisher> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        var sender = _client.CreateSender(topic);
        try
        {
            var json = JsonSerializer.Serialize(message);
            var sbMessage = new ServiceBusMessage(json)
            {
                ContentType = "application/json"
            };

            var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            sbMessage.CorrelationId = correlationId;

            await sender.SendMessageAsync(sbMessage, cancellationToken);
            s_published.Add(1, new TagList { { "queue", topic }, { "event_type", typeof(T).Name } });
            _logger.LogInformation("Published {EventType} to queue {Queue} with CorrelationId {CorrelationId}",
                typeof(T).Name, topic, correlationId);
        }
        catch (Exception ex)
        {
            s_errors.Add(1, new TagList { { "queue", topic }, { "event_type", typeof(T).Name } });
            _logger.LogError(ex, "Failed to publish {EventType} to queue {Queue}", typeof(T).Name, topic);
            throw;
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }
}
