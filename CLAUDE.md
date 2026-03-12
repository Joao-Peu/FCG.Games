# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build
dotnet build FCG.Games.sln

# Run API locally (http://localhost:5105)
dotnet run --project src/FCG.Games.Api/FCG.Games.Api.csproj

# Run all tests
dotnet test tests/FCG.Games.Domain.Tests/FCG.Games.Domain.Tests.csproj
dotnet test tests/FCG.Games.Application.Tests/FCG.Games.Application.Tests.csproj

# Run a single test by method name
dotnet test tests/FCG.Games.Application.Tests/FCG.Games.Application.Tests.csproj --filter MethodName

# Run tests by class
dotnet test tests/FCG.Games.Application.Tests/FCG.Games.Application.Tests.csproj --filter FullyQualifiedName~PlaceOrderCommandHandlerTests

# Docker
docker build -t fcg-games .
```

## Architecture

.NET 8 ASP.NET Core Web API microservice for games catalog and purchase system with CQRS. Portuguese-language project (PosTech FIAP).

**Solution structure (6 projects):**
```
src/
├── FCG.Games.Domain/           # Entities, VOs (Result/Error), Events, Interfaces — zero NuGet deps
├── FCG.Games.Application/      # Commands, Queries, Handlers (manual CQRS, no MediatR), DTOs
├── FCG.Games.Infrastructure/   # EF Core (SQL Server), Service Bus, Repositories, UnitOfWork
└── FCG.Games.Api/              # Controllers, Middleware, Startup (JWT + Swagger + OpenTelemetry)
tests/
├── FCG.Games.Domain.Tests/     # Result/Error value object tests
└── FCG.Games.Application.Tests/ # Handler tests with Moq
```

**Dependencies:** Domain ← Application ← Infrastructure; Api → Application + Infrastructure

### Key layers

- **Domain** — Entities (`Game`, `OrderGame`, `UserLibraryEntry`, `AuditEvent`), enums (`OrderStatus`), value objects (`Result<T>`, `Error`), domain events (`OrderPlacedEvent`, `PaymentProcessedEvent`), repository interfaces.
- **Application** — CQRS with `IQueryHandler<TQuery, TResult>` and `ICommandHandler<TCommand, TResult>`. Handlers: `ListGamesQueryHandler`, `GetRecommendationsQueryHandler` (top 10 by price placeholder), `PlaceOrderCommandHandler` (validates ownership/pending, publishes event). Event handler: `PaymentProcessedHandler`.
- **Infrastructure** — `AppDbContext` with automatic audit trail in `SaveChangesAsync`. EF Core configurations with fluent API. `ServiceBusEventPublisher` (direct, no reflection) + `ServiceBusConsumerService` (BackgroundService for payment events). `NoOpEventPublisher` fallback.
- **Api** — 3 endpoints (all `[Authorize]`): `GET /api/games`, `POST /api/games/{gameId}/purchase`, `GET /api/games/recommendations`. JWT with configurable validation. Swagger with Bearer security definition (API Management compatible). `CorrelationIdMiddleware` for request tracing.

### Key behaviors

- Purchase creates `OrderGame` (PendingPayment) and publishes `OrderPlacedEvent` to configurable Service Bus topic.
- `PaymentProcessedHandler` listens for payment results: Approved → completes order + adds to user library; Rejected → marks PaymentFailed.
- Result pattern (`Result<T>`) maps to HTTP: Success=200/202, GameNotFound=404, AlreadyOwned/PendingOrder=409.
- OpenTelemetry tracing with AspNetCore, HTTP, and SQL Client instrumentation.
- `Database.EnsureCreated()` at startup (dev/demo mode).

## Testing

Tests use xUnit + Moq. Test projects:
- `FCG.Games.Domain.Tests` — `ResultTests` (Result/Error value objects)
- `FCG.Games.Application.Tests` — `PlaceOrderCommandHandlerTests` (4 cases), `ListGamesQueryHandlerTests`, `GetRecommendationsQueryHandlerTests`, `PaymentProcessedHandlerTests` (Approved/Rejected paths)

## Configuration

Key settings via `appsettings.json` or environment variables:
- `ConnectionStrings:DefaultConnection` — Azure SQL Serverless connection string
- `ServiceBus:ConnectionString` — Azure Service Bus (optional, falls back to NoOp)
- `ServiceBus:OrderPlacedTopic` — topic for order events (default: "order-placed")
- `ServiceBus:PaymentProcessedTopic` / `PaymentProcessedSubscription` — payment event consumption
- `AzureMonitor:ConnectionString` — Application Insights connection string (optional, disables Azure Monitor exporter when empty)
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` — JWT authentication (all configurable)
