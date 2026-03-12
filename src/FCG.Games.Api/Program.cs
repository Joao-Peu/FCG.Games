using System.Text;
using FCG.Games.Api.Middleware;
using FCG.Games.Application;
using FCG.Games.Infrastructure;
using FCG.Games.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var services = builder.Services;

// Controllers
services.AddControllers();
services.AddEndpointsApiExplorer();

// Swagger with Bearer security definition (API Management compatible)
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FCG.Games API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
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

// OpenTelemetry + Azure Monitor
var azureMonitorConnectionString = configuration["AzureMonitor:ConnectionString"];
if (!string.IsNullOrEmpty(azureMonitorConnectionString))
{
    services.AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = azureMonitorConnectionString;
        });
}

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

// Ensure DB created (for dev/demo) — retries handle Azure SQL Serverless cold-start
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var strategy = db.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(async () => { await db.Database.EnsureCreatedAsync(); });
}

app.Run();
