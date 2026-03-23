using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FCG.Games.Api.Diagnostics;

public static class GameMetrics
{
    public const string ServiceName = "FCG.Games";

    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    // HTTP request metrics
    public static readonly Counter<long> HttpRequestsTotal = Meter.CreateCounter<long>(
        "fcg.http.requests.total",
        description: "Total de requisições HTTP recebidas");

    public static readonly Histogram<double> HttpRequestDuration = Meter.CreateHistogram<double>(
        "fcg.http.request.duration",
        unit: "ms",
        description: "Duração das requisições HTTP em milissegundos");

    // Purchase/Order metrics
    public static readonly Counter<long> PurchaseRequests = Meter.CreateCounter<long>(
        "fcg.orders.purchase_requests",
        description: "Total de solicitações de compra");

    public static readonly Counter<long> PurchaseSucceeded = Meter.CreateCounter<long>(
        "fcg.orders.purchase_succeeded",
        description: "Compras aceitas (PendingPayment)");

    public static readonly Counter<long> PurchaseFailed = Meter.CreateCounter<long>(
        "fcg.orders.purchase_failed",
        description: "Compras rejeitadas (jogo não encontrado, já possui, pedido pendente)");

    public static readonly Counter<long> PaymentsApproved = Meter.CreateCounter<long>(
        "fcg.orders.payments_approved",
        description: "Pagamentos aprovados");

    public static readonly Counter<long> PaymentsRejected = Meter.CreateCounter<long>(
        "fcg.orders.payments_rejected",
        description: "Pagamentos rejeitados");

    // Service Bus metrics
    public static readonly Counter<long> ServiceBusMessagesPublished = Meter.CreateCounter<long>(
        "fcg.servicebus.messages_published",
        description: "Mensagens publicadas no Service Bus");

    public static readonly Counter<long> ServiceBusPublishErrors = Meter.CreateCounter<long>(
        "fcg.servicebus.publish_errors",
        description: "Erros ao publicar no Service Bus");

    public static readonly Counter<long> ServiceBusMessagesConsumed = Meter.CreateCounter<long>(
        "fcg.servicebus.messages_consumed",
        description: "Mensagens consumidas do Service Bus");

    public static readonly Counter<long> ServiceBusConsumeErrors = Meter.CreateCounter<long>(
        "fcg.servicebus.consume_errors",
        description: "Erros ao processar mensagens do Service Bus");

    public static readonly Histogram<double> ServiceBusProcessingDuration = Meter.CreateHistogram<double>(
        "fcg.servicebus.processing.duration",
        unit: "ms",
        description: "Duração do processamento de mensagens do Service Bus");

    // Game catalog metrics
    public static readonly Counter<long> GamesCatalogQueries = Meter.CreateCounter<long>(
        "fcg.games.catalog_queries",
        description: "Consultas ao catálogo de jogos");

    public static readonly Counter<long> RecommendationQueries = Meter.CreateCounter<long>(
        "fcg.games.recommendation_queries",
        description: "Consultas de recomendações");
}
