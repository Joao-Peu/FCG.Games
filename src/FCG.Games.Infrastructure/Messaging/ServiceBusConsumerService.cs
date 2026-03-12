using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FCG.Games.Application.EventHandlers;
using FCG.Games.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FCG.Games.Infrastructure.Messaging;

public class ServiceBusConsumerService : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ServiceBusConsumerService> _logger;
    private readonly string _topic;
    private readonly string _subscription;
    private ServiceBusProcessor? _processor;

    public ServiceBusConsumerService(
        ServiceBusClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<ServiceBusConsumerService> logger,
        string topic,
        string subscription)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _topic = topic;
        _subscription = subscription;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = _client.CreateProcessor(_topic, _subscription, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<PaymentProcessedEvent>(args.Message.Body.ToString());
            if (@event is null) return;

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<PaymentProcessedEvent>>();
            await handler.HandleAsync(@event, args.CancellationToken);

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            _logger.LogInformation("Processed payment event for OrderId {OrderId} with status {Status}",
                @event.OrderId, @event.Status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process payment event, abandoning message {MessageId}",
                args.Message.MessageId);
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processor error: {Source}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
