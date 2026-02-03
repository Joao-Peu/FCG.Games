# FCG.Games

Microserviço de Games - .NET 8, ASP.NET Core Web API

Resumo rápido:
- CRUD básico para jogos (Create, GetById, Search)
- Purchase intent que publica evento `PurchaseRequested` em um tópico
- Audit log (tabela `AuditEvents`) para alterações em jogos e pedidos
- Busca otimizada com provider configurável: Azure Cognitive Search (via env) ou fallback SQL
- Autenticação via JWT (apenas para endpoints, health é aberto)
- OpenTelemetry instrumentação
- Pronto para execução em Docker e Azure

Endpoints principais:
- POST `/api/games` - criar jogo (JWT)
- GET `/api/games/{id}` - obter jogo por id (JWT)
- GET `/api/games/search?query=&genre=&minPrice=&maxPrice=&page=&pageSize=` - busca (JWT)
- POST `/api/games/{gameId}/purchase` - iniciar intenção de compra (publica evento) (JWT)
- GET `/api/games/recommendations` - recomendações simples (JWT)

Eventos:
- `PurchaseRequested` - publicado quando usuário inicia compra. Contém PurchaseId, UserId, GameId, Price, Currency, RequestedAtUtc, CorrelationId.

Audit:
- Tabela `AuditEvents` salva ações Add/Update para entidades (incluindo jogos e pedidos).

Search provider:
- Configurar `AZURE_SEARCH_ENDPOINT` e `AZURE_SEARCH_KEY` como variáveis de ambiente para usar Azure Cognitive Search (implementação placeholder hoje).
- Caso contrário, usa pesquisa via SQL com `LIKE` (fallback). For demo local, use SQL fallback.

Service Bus:
- Configure `ServiceBus:ConnectionString` in appsettings or `SERVICE_BUS_CONNECTION_STRING` env var.

Correlation:
- `x-correlation-id` header is propagated to responses and event correlation.

Run (local with Docker):
- See `docker-compose.yml` to run API + SQL Server

Testing:
- Unit tests present in the solution (project `FCG.Games.Tests`).

Notes:
- Azure integrations (Search) are placeholders but interfaces are ready for production wiring.
- This project is designed for demonstrative purposes and should be hardened for production (secrets, key management, detailed telemetry collectors, retries, dead-lettering for messaging, etc.).
