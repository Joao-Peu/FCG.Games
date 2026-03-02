# FCG.Games

Microservi�o de Games - .NET 8, ASP.NET Core Web API

Resumo r�pido:
- CRUD b�sico para jogos (Create, GetById, Search)
- Purchase intent que publica evento `PurchaseRequested` em um t�pico
- Audit log (tabela `AuditEvents`) para altera��es em jogos e pedidos
- Busca otimizada com provider configur�vel: Azure Cognitive Search (via env) ou fallback SQL
- Autentica��o via JWT (apenas para endpoints, health � aberto)
- OpenTelemetry instrumenta��o
- Pronto para execu��o em Docker e Azure

Endpoints principais:
- POST `/api/games` - criar jogo (JWT)
- GET `/api/games/{id}` - obter jogo por id (JWT)
- GET `/api/games/search?query=&genre=&minPrice=&maxPrice=&page=&pageSize=` - busca (JWT)
- POST `/api/games/{gameId}/purchase` - iniciar inten��o de compra (publica evento) (JWT)
- GET `/api/games/recommendations` - recomenda��es simples (JWT)

Eventos:
- `PurchaseRequested` - publicado quando usu�rio inicia compra. Cont�m PurchaseId, UserId, GameId, Price, Currency, RequestedAtUtc, CorrelationId.

Audit:
- Tabela `AuditEvents` salva a��es Add/Update para entidades (incluindo jogos e pedidos).

Search provider:
- Configurar `AZURE_SEARCH_ENDPOINT` e `AZURE_SEARCH_KEY` como vari�veis de ambiente para usar Azure Cognitive Search (implementa��o placeholder hoje).
- Caso contr�rio, usa pesquisa via SQL com `LIKE` (fallback). For demo local, use SQL fallback.

Service Bus:
- Configure `ServiceBus:ConnectionString` in appsettings or `SERVICE_BUS_CONNECTION_STRING` env var.

Correlation:
- `x-correlation-id` header is propagated to responses and event correlation.

Run (local with Docker):
- See `docker-compose.yml` to run API + SQL Server

Testing:
- Unit tests are located in the `FCG.Games.Tests` project under the `Tests` folder.
- Run them with the .NET CLI from the FCG.Games folder:
  ```bash
  dotnet test FCG.Games.Tests/FCG.Games.Tests.csproj
  ```
- The tests cover core services (`GameService`, `PurchaseService`), the EF `AppDbContext` auditing logic and basic controller behavior.
- To add new tests, create additional classes in the `Tests` folder, reference `Moq` or create simple stubs, and rely on `Microsoft.EntityFrameworkCore.InMemory` for database-related code.
- You can execute an individual test file or group by using the `--filter` option.

Notes:
- Azure integrations (Search) are placeholders but interfaces are ready for production wiring.
- This project is designed for demonstrative purposes and should be hardened for production (secrets, key management, detailed telemetry collectors, retries, dead-lettering for messaging, etc.).
