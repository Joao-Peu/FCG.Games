using System.Diagnostics;
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
    private readonly string _queue;
    private ServiceBusProcessor? _processor;

    public ServiceBusConsumerService(
        ServiceBusClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<ServiceBusConsumerService> logger,
        string queue)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = _client.CreateProcessor(_queue, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var correlationId = args.Message.CorrelationId ?? Guid.NewGuid().ToString();

        using var activity = new Activity("process-payment-event");
        activity.SetParentId(correlationId);
        activity.Start();

        using var logScope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });

        try
        {
            var @event = JsonSerializer.Deserialize<PaymentProcessedEvent>(args.Message.Body.ToString());
            if (@event is null) return;

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<PaymentProcessedEvent>>();
            await handler.HandleAsync(@event, args.CancellationToken);

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            _logger.LogInformation("Processed payment event for OrderId {OrderId} with status {Status} CorrelationId {CorrelationId}",
                @event.OrderId, @event.Status, correlationId);
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
