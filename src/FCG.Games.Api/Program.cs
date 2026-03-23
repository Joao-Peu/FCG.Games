using System.Text;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using FCG.Games.Api.Diagnostics;
using FCG.Games.Api.Middleware;
using FCG.Games.Application;
using FCG.Games.Infrastructure;
using FCG.Games.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;
    var services = builder.Services;

    // Serilog
    builder.Host.UseSerilog((context, svcProvider, loggerConfig) => loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(svcProvider)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("ServiceName", "FCG.Games")
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.Conditional(
            _ => !string.IsNullOrEmpty(context.Configuration["ApplicationInsights:ConnectionString"]),
            wt => wt.ApplicationInsights(
                context.Configuration["ApplicationInsights:ConnectionString"],
                new TraceTelemetryConverter())));

    // OpenTelemetry
    var azMonitorCs = configuration["ApplicationInsights:ConnectionString"];
    var otelBuilder = builder.Services.AddOpenTelemetry()
        .ConfigureResource(res => res.AddService(
            serviceName: GameMetrics.ServiceName,
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
        .WithTracing(tracing => tracing
            .AddSource(GameMetrics.ServiceName)
            .AddAspNetCoreInstrumentation(opts =>
            {
                opts.RecordException = true;
                opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
            })
            .AddHttpClientInstrumentation(opts => opts.RecordException = true)
)
        .WithMetrics(metrics => metrics
            .AddMeter(GameMetrics.ServiceName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation());

    if (!string.IsNullOrEmpty(azMonitorCs))
        otelBuilder.UseAzureMonitor(opts => opts.ConnectionString = azMonitorCs);

    // Health Checks
    var healthBuilder = services.AddHealthChecks();
    var dbCs = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(dbCs))
        healthBuilder.AddSqlServer(dbCs, name: "sqlserver", tags: new[] { "db", "ready" });
    var sbCs = configuration["ServiceBus:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(sbCs))
        healthBuilder.AddAzureServiceBusQueue(sbCs,
            queueName: configuration["ServiceBus:OrderPlacedQueue"] ?? "order-placed",
            name: "servicebus", tags: new[] { "messaging", "ready" });

    // Controllers
    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Insira o token JWT. Exemplo: eyJhbGciOiJIUzI1NiIs..."
        });
        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Application + Infrastructure layers
    services.AddApplication();
    services.AddInfrastructure(configuration);

    // JWT Authentication
    var jwtKey = configuration["Jwt:Key"] ?? string.Empty;
    var jwtIssuer = configuration["Jwt:Issuer"] ?? string.Empty;
    var jwtAudience = configuration["Jwt:Audience"] ?? string.Empty;

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
                ValidIssuer = jwtIssuer,
                ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = !string.IsNullOrEmpty(jwtKey),
                IssuerSigningKey = !string.IsNullOrEmpty(jwtKey)
                    ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    : null
            };
        });
    services.AddAuthorization();

    // Middleware
    services.AddSingleton<CorrelationIdMiddleware>();
    services.AddSingleton<MetricsMiddleware>();

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<MetricsMiddleware>();
    app.UseSerilogRequestLogging();

    app.UseSwagger();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("FCG.Games API");
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
        options.WithPreferredScheme("Bearer");
        options.WithHttpBearerAuthentication(bearer =>
        {
            bearer.Token = string.Empty;
        });
    });

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Health check endpoints
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false, // liveness: nenhuma dependência
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });

    // Ensure DB created + seed (for dev/demo) — retries handle Azure SQL Serverless cold-start
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () => { await db.Database.EnsureCreatedAsync(); });
        var seederLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseSeeder));
        await DatabaseSeeder.SeedAsync(db, seederLogger);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
