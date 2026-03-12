using FCG.Games.Application.Abstractions;
using FCG.Games.Application.Commands;
using FCG.Games.Application.DTOs;
using FCG.Games.Application.EventHandlers;
using FCG.Games.Application.Queries;
using FCG.Games.Domain.Events;
using FCG.Games.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Games.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListGamesQuery, Result<IReadOnlyList<GameDto>>>, ListGamesQueryHandler>();
        services.AddScoped<IQueryHandler<GetRecommendationsQuery, Result<IReadOnlyList<GameDto>>>, GetRecommendationsQueryHandler>();
        services.AddScoped<ICommandHandler<PlaceOrderCommand, Result<PurchaseResultDto>>, PlaceOrderCommandHandler>(sp =>
        {
            var config = sp.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
            var topic = config?["ServiceBus:OrderPlacedTopic"] ?? "order-placed";
            return new PlaceOrderCommandHandler(
                sp.GetRequiredService<FCG.Games.Domain.Interfaces.IGameRepository>(),
                sp.GetRequiredService<FCG.Games.Domain.Interfaces.IUserLibraryRepository>(),
                sp.GetRequiredService<FCG.Games.Domain.Interfaces.IOrderRepository>(),
                sp.GetRequiredService<FCG.Games.Domain.Interfaces.IUnitOfWork>(),
                sp.GetRequiredService<FCG.Games.Domain.Interfaces.IEventPublisher>(),
                topic);
        });
        services.AddScoped<IEventHandler<PaymentProcessedEvent>, PaymentProcessedHandler>();

        return services;
    }
}
