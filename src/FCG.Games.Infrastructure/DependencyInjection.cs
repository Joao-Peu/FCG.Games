using Azure.Messaging.ServiceBus;
using FCG.Games.Domain.Interfaces;
using FCG.Games.Infrastructure.Messaging;
using FCG.Games.Infrastructure.Persistence;
using FCG.Games.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FCG.Games.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database (Azure SQL Serverless — retry on transient errors including cold-start wake-up)
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(60);
            }));

        // Repositories
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUserLibraryRepository, UserLibraryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Messaging
        var sbConnectionString = configuration["ServiceBus:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(sbConnectionString))
        {
            services.AddSingleton(_ => new ServiceBusClient(sbConnectionString)); 
            services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();

            var queue = configuration["ServiceBus:PaymentProcessedQueue"] ?? "payments-processed";
            services.AddHostedService(sp => new ServiceBusConsumerService(
                sp.GetRequiredService<ServiceBusClient>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<ServiceBusConsumerService>>(),
                queue));
        }
        else
        {
            services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
        }

        return services;
    }
}
