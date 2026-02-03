using System.Reflection;
using OpenTelemetry; // add Sdk reference
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using FCG.Games.Data;
using FCG.Games.Services;
using FCG.Games.Search;
using FCG.Games.Messaging;
using FCG.Games.Middleware;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var services = builder.Services;

// Configuration
services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    var xmlFile = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".xml");
    if (File.Exists(xmlFile)) c.IncludeXmlComments(xmlFile);
});

// Database
var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Server=sqlserver,1433;Database=FCGGames;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";
services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionString));

// Search provider selection via env var: AZURE_SEARCH_ENDPOINT (if present use Azure)
services.AddScoped<IGameSearchProvider>(sp =>
{
    var env = Environment.GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT");
    if (!string.IsNullOrEmpty(env)) return new AzureSearchProvider(env, Environment.GetEnvironmentVariable("AZURE_SEARCH_KEY"));
    return new SqlSearchProvider(sp.GetRequiredService<AppDbContext>());
});

// Service Bus client
var sbConnection = configuration["ServiceBus:ConnectionString"] ?? Environment.GetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING");
if (!string.IsNullOrEmpty(sbConnection))
{
    // create client via reflection to avoid hard dependency when package not available in some environments
    try
    {
        var clientType = Type.GetType("Azure.Messaging.ServiceBus.ServiceBusClient, Azure.Messaging.ServiceBus");
        if (clientType != null)
        {
            var client = Activator.CreateInstance(clientType, sbConnection);
            services.AddSingleton(clientType, client!);
            services.AddSingleton<IPublisher, ServiceBusPublisher>(sp => new ServiceBusPublisher((dynamic)sp.GetRequiredService(clientType)));
        }
        else
        {
            services.AddSingleton<IPublisher, NoOpPublisher>();
        }
    }
    catch
    {
        services.AddSingleton<IPublisher, NoOpPublisher>();
    }
}
else
{
    services.AddSingleton<IPublisher, NoOpPublisher>();
}

// Application services
services.AddScoped<IGameService, GameService>();
services.AddScoped<IPurchaseService, PurchaseService>();

// Auth
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = false
        };
    });

services.AddAuthorization();

// OpenTelemetry: build tracer provider
OpenTelemetry.Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FCG.Games"))
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddSqlClientInstrumentation()
    .AddSource("FCG.Games")
    .SetSampler(new AlwaysOnSampler())
    .Build();

// Middleware
services.AddSingleton<CorrelationIdMiddleware>();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Ensure DB created (for demo/local)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
