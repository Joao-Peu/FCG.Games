# FCG.Games API

Microsserviço de catálogo de jogos e sistema de compras, desenvolvido com .NET 8 e ASP.NET Core Web API. Projeto da **Fase 3 do Tech Challenge — PosTech FIAP**.

## Arquitetura

O projeto segue **Clean Architecture** com **CQRS** (sem MediatR), organizado em 4 camadas:

```
src/
├── FCG.Games.Domain/           # Entidades, Value Objects, Eventos, Interfaces (zero dependências NuGet)
├── FCG.Games.Application/      # Commands, Queries, Handlers, DTOs
├── FCG.Games.Infrastructure/   # EF Core (SQL Server), Azure Service Bus, Repositórios, UnitOfWork
└── FCG.Games.Api/              # Controllers, Middleware, Startup (JWT + Swagger + OpenTelemetry)
tests/
├── FCG.Games.Domain.Tests/     # Testes de Value Objects (Result/Error)
├── FCG.Games.Application.Tests/# Testes de Handlers com NSubstitute
└── FCG.Games.Infrastructure.Tests/ # Testes do AppDbContext (audit trail)
```

**Fluxo de dependências:** Domain ← Application ← Infrastructure; Api → Application + Infrastructure

## Endpoints

Todos os endpoints exigem autenticação JWT (`[Authorize]`).

| Método | Rota | Descrição | Resposta |
|--------|------|-----------|----------|
| `GET` | `/api/games` | Lista todos os jogos do catálogo | `200` com `GameDto[]` |
| `POST` | `/api/games/{gameId}/purchase` | Solicita compra de um jogo | `202` Accepted / `404` Not Found / `409` Conflict |
| `GET` | `/api/games/recommendations` | Top 10 jogos recomendados (por preço) | `200` com `GameDto[]` |

### Detalhes da Compra (`POST /api/games/{gameId}/purchase`)

- Extrai `userId` do token JWT (claim `sub` ou `NameIdentifier`)
- Aceita header `x-correlation-id` para rastreamento
- Cria `OrderGame` com status `PendingPayment`
- Publica `OrderPlacedEvent` no Azure Service Bus
- Retorna `202 Accepted` com `{ orderId, status }`

**Erros tratados:**
- `404` — Jogo não encontrado (`Game.NotFound`)
- `409` — Usuário já possui o jogo (`Game.AlreadyOwned`) ou já tem pedido pendente (`Order.Pending`)

## Domínio

### Entidades

| Entidade | Campos principais |
|----------|-------------------|
| `Game` | Id, Title, Description, Genre, Price, Currency, CreatedAtUtc, UpdatedAtUtc |
| `OrderGame` | Id, UserId, GameId, Price, Currency, Status, IsProcessed, CorrelationId |
| `UserLibraryEntry` | Id, UserId, GameId, CreatedAt |
| `AuditEvent` | EventId, EntityName, EntityKey, Action, Data, CreatedAtUtc |

### Eventos de Domínio

| Evento | Descrição |
|--------|-----------|
| `OrderPlacedEvent` | Emitido ao criar pedido (OrderId, UserId, GameId, Price, Currency, CorrelationId) |
| `PaymentProcessedEvent` | Recebido do serviço de pagamento (OrderId, UserId, GameId, Price, Status) |

### Fluxo de Compra

```
[Cliente] → POST /purchase → [PlaceOrderCommandHandler]
    → Cria OrderGame (PendingPayment)
    → Publica OrderPlacedEvent no Service Bus (topic: order-placed)

[Service Bus] → payment-events/games-service → [ServiceBusConsumerService]
    → [PaymentProcessedHandler]
        → Status "Approved": completa pedido + adiciona à biblioteca do usuário
        → Status "Rejected": marca pedido como PaymentFailed
```

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local ou Azure SQL Serverless)
- Azure Service Bus (opcional — sem configuração, usa `NoOpEventPublisher`)
- Azure Application Insights (opcional — sem configuração, sobe sem exporter)

## Configuração

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:<server>.database.windows.net;Database=<db>;..."
  },
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://<namespace>.servicebus.windows.net/;...",
    "OrderPlacedTopic": "order-placed",
    "PaymentProcessedTopic": "payment-events",
    "PaymentProcessedSubscription": "games-service"
  },
  "AzureMonitor": {
    "ConnectionString": "InstrumentationKey=<key>;IngestionEndpoint=..."
  },
  "Jwt": {
    "Key": "<chave-secreta>",
    "Issuer": "<issuer>",
    "Audience": "<audience>"
  }
}
```

### Variáveis de Ambiente (produção)

O .NET usa `__` como separador de seções:

```bash
ConnectionStrings__DefaultConnection="Server=tcp:..."
ServiceBus__ConnectionString="Endpoint=sb://..."
AzureMonitor__ConnectionString="InstrumentationKey=..."
Jwt__Key="<chave-secreta>"
Jwt__Issuer="<issuer>"
Jwt__Audience="<audience>"
```

### Comportamento sem configuração

| Configuração | Vazia/ausente | Comportamento |
|--------------|---------------|---------------|
| `ConnectionStrings:DefaultConnection` | sim | Falha ao iniciar (requer SQL Server) |
| `ServiceBus:ConnectionString` | sim | Usa `NoOpEventPublisher` (eventos descartados) |
| `AzureMonitor:ConnectionString` | sim | App sobe sem exporter (sem telemetria) |
| `Jwt:Key` | sim | Validação de signing key desabilitada |

## Build & Run

```bash
# Restaurar dependências
dotnet restore FCG.Games.sln

# Build
dotnet build FCG.Games.sln

# Executar API (http://localhost:5105)
dotnet run --project src/FCG.Games.Api/FCG.Games.Api.csproj
```

> No startup, o app executa `Database.EnsureCreated()` automaticamente (modo dev/demo), com retry para suportar o cold-start do Azure SQL Serverless.

### Azure SQL Serverless

O projeto está configurado para Azure SQL Serverless, que pausa automaticamente após inatividade. Para lidar com o wake-up (~1 minuto):

- **`EnableRetryOnFailure`** — 5 tentativas com até 30s de delay entre cada, cobrindo erros transitórios (incluindo erro 40613 do cold-start)
- **`CommandTimeout(60)`** — 60s de timeout por comando para acomodar o tempo de wake-up
- **`EnsureCreated` com `ExecutionStrategy`** — o startup usa a mesma política de retry, evitando falha se o banco estiver pausado

## Testes

O projeto possui **17 testes** usando xUnit + NSubstitute:

```bash
# Executar todos os testes
dotnet test FCG.Games.sln

# Testes de domínio (6 testes — Result/Error)
dotnet test tests/FCG.Games.Domain.Tests/FCG.Games.Domain.Tests.csproj

# Testes de aplicação (8 testes — Handlers)
dotnet test tests/FCG.Games.Application.Tests/FCG.Games.Application.Tests.csproj

# Testes de infraestrutura (3 testes — Audit Trail)
dotnet test tests/FCG.Games.Infrastructure.Tests/FCG.Games.Infrastructure.Tests.csproj

# Filtrar por classe
dotnet test tests/FCG.Games.Application.Tests/FCG.Games.Application.Tests.csproj \
  --filter FullyQualifiedName~PlaceOrderCommandHandlerTests

# Filtrar por método
dotnet test tests/FCG.Games.Application.Tests/FCG.Games.Application.Tests.csproj \
  --filter HandleAsync_HappyPath_CreatesOrderAndPublishesEvent
```

### Cobertura de Testes

| Projeto | Classe | Testes |
|---------|--------|--------|
| Domain | `ResultTests` | 6 (Success/Failure genérico, Error.None, erros predefinidos) |
| Application | `PlaceOrderCommandHandlerTests` | 4 (happy path, game not found, already owned, pending order) |
| Application | `PaymentProcessedHandlerTests` | 2 (Approved, Rejected) |
| Application | `ListGamesQueryHandlerTests` | 1 (mapeamento de jogos) |
| Application | `GetRecommendationsQueryHandlerTests` | 1 (top 10 por preço) |
| Infrastructure | `AppDbContextTests` | 3 (audit para insert, update, ignora AuditEvent) |

## Docker

```bash
# Build da imagem
docker build -t fcg-games .

# Executar container
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Server=tcp:..." \
  -e ServiceBus__ConnectionString="Endpoint=sb://..." \
  -e AzureMonitor__ConnectionString="InstrumentationKey=..." \
  -e Jwt__Key="<chave>" \
  fcg-games
```

## Observabilidade

### Azure Monitor (Application Insights)

O projeto utiliza `Azure.Monitor.OpenTelemetry.AspNetCore` para exportar traces, métricas e logs para o Application Insights. Quando `AzureMonitor:ConnectionString` é configurada, o `UseAzureMonitor()` ativa automaticamente:

- Instrumentação ASP.NET Core (requests HTTP)
- Instrumentação HTTP Client (chamadas externas)
- Instrumentação SQL Client (queries ao banco)
- Métricas de performance

### Correlation ID

O `CorrelationIdMiddleware` propaga o header `x-correlation-id` entre requests, permitindo rastreamento end-to-end no fluxo de compra.

### Logging Estruturado

O `ServiceBusEventPublisher` e o `ServiceBusConsumerService` utilizam logging estruturado com `ILogger`, registrando:

- Publicação de eventos (tipo, tópico)
- Processamento de pagamentos (OrderId, Status)
- Erros e falhas com contexto completo

## Tecnologias

| Tecnologia | Uso |
|------------|-----|
| .NET 8 / ASP.NET Core | Framework da API |
| Entity Framework Core 8 | ORM (Azure SQL Serverless, com retry para cold-start) |
| Azure Service Bus | Mensageria assíncrona |
| Azure Monitor / Application Insights | Observabilidade (OpenTelemetry) |
| JWT Bearer | Autenticação |
| Swagger / OpenAPI | Documentação da API |
| xUnit + NSubstitute | Testes unitários |
| Docker | Containerização |
