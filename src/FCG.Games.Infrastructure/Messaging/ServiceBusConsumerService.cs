using System.Diagnostics;
using System.Diagnostics.Metrics;
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
    private static readonly Meter s_meter = new("FCG.Games");
    private static readonly Counter<long> s_consumed = s_meter.CreateCounter<long>(
        "fcg.servicebus.messages_consumed", description: "Mensagens consumidas do Service Bus");
    private static readonly Counter<long> s_errors = s_meter.CreateCounter<long>(
        "fcg.servicebus.consume_errors", description: "Erros ao processar mensagens do Service Bus");
    private static readonly Histogram<double> s_duration = s_meter.CreateHistogram<double>(
        "fcg.servicebus.processing.duration", unit: "ms", description: "Duração do processamento de mensagens");
    private static readonly Counter<long> s_paymentsApproved = s_meter.CreateCounter<long>(
        "fcg.orders.payments_approved", description: "Pagamentos aprovados");
    private static readonly Counter<long> s_paymentsRejected = s_meter.CreateCounter<long>(
        "fcg.orders.payments_rejected", description: "Pagamentos rejeitados");

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

        var sw = Stopwatch.StartNew();
        try
        {
            var @event = JsonSerializer.Deserialize<PaymentProcessedEvent>(args.Message.Body.ToString());
            if (@event is null) return;

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<PaymentProcessedEvent>>();
            await handler.HandleAsync(@event, args.CancellationToken);

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            s_consumed.Add(1, new TagList { { "queue", _queue }, { "status", @event.Status } });

            if (string.Equals(@event.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                s_paymentsApproved.Add(1);
            else
                s_paymentsRejected.Add(1);

            _logger.LogInformation("Processed payment event for OrderId {OrderId} with status {Status} CorrelationId {CorrelationId}",
                @event.OrderId, @event.Status, correlationId);
        }
        catch (Exception ex)
        {
            s_errors.Add(1, new TagList { { "queue", _queue } });
            _logger.LogWarning(ex, "Failed to process payment event, abandoning message {MessageId}",
                args.Message.MessageId);
            await args.AbandonMessageAsync(args.Message);
        }
        finally
        {
            sw.Stop();
            s_duration.Record(sw.Elapsed.TotalMilliseconds, new TagList { { "queue", _queue } });
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
