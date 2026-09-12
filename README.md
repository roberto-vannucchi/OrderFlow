# OrderFlow

A .NET portfolio project — a deliberately simple domain (orders) paired with a technology-rich stack: CQRS, messaging, caching, observability.

## Architecture

Synchronous flow (HTTP request):

```
HTTP client
        │
        ▼
┌────────────────┐
│      Api       │
└────────────────┘   HTTP endpoints
        │
        ▼
┌────────────────┐
│  Application   │
└────────────────┘   CQRS (Wolverine) + Mapster
        │
        ▼
┌────────────────┐
│ Infrastructure │
└────────────────┘   domain + EF Core
        │
        ▼
┌────────────────┐
│   PostgreSQL   │
└────────────────┘
```

Asynchronous flow, triggered by Application after an order is created or its status changes:

```
┌────────────────┐
│  Application   │
└────────────────┘   publishes OrderCreated / OrderStatusChanged
        │
        ▼
┌────────────────┐
│    RabbitMQ    │
└────────────────┘   planned
        │
        ▼
┌────────────────┐
│     Redis      │
└────────────────┘   planned — order-by-status cache,
                      invalidated by the queue consumer
```

## Stack

- **.NET 10** / C#
- **PostgreSQL** via Docker Compose
- **EF Core 10** + Npgsql, explicit mapping via Fluent API (`IEntityTypeConfiguration`)
- **Wolverine** — CQRS (Command/Query handlers) and messaging in a single open-source framework
- **Mapster** — DTO ↔ Command/Domain mapping, convention-based and explicit (`IRegister` + `Scan`)
- **RabbitMQ** (planned) — async queue
- **Redis** (planned) — cache-aside for orders by status, invalidated via a queue event
- **OpenTelemetry** (planned) — tracing/metrics/logs
- **Testcontainers** (planned) — integration tests against real Postgres/RabbitMQ

## Roadmap

### Done

- [x] Solution structure (`.slnx`, `src/Api`, `src/Application`, `src/Infrastructure`)
- [x] `Order` domain model — rich model (private setters, factory method, business rules enforced through methods)
- [x] Explicit EF Core mapping (table/column names, decimal precision, enum conversion, index)
- [x] Postgres via `docker-compose.yml` with a persistent volume
- [x] First migration applied, end-to-end round trip validated (temporary endpoint)
- [x] Git repository + GitHub set up
- [x] CQRS with Wolverine — real `CreateOrder` (Command) and `GetOrder` (Query), replacing the temporary endpoints
- [x] DTO ↔ Command/Domain mapper with Mapster — convention-based and explicit mapping (`OrderReference` computed via `IRegister`)

### Planned

- [ ] Async queue (RabbitMQ via Wolverine's transport) — `OrderCreated`/`OrderStatusChanged` events published on order creation and status changes
- [ ] Redis read cache — `GetOrdersByStatus` with cache-aside, invalidated by the queue consumer on status change (demonstrates the pattern; the project's data volume doesn't call for a real performance win)
- [ ] Observability — OpenTelemetry (traces, metrics, logs) + dashboard (Grafana or Seq, TBD)
- [ ] Resilience — Polly (retry/circuit breaker) on the queue consumer
- [ ] Authentication — JWT Bearer + Keycloak (not ASP.NET Core Identity — see technical decisions below)
- [ ] Integration tests with Testcontainers (real Postgres/RabbitMQ, no mocks)
- [ ] API documentation — OpenAPI + Scalar
- [ ] CI — GitHub Actions (build, test, publish image)
- [ ] Full containerization via Docker Compose (API + infra in a single `up`)
- [ ] AI assistant configuration in the repo (`CLAUDE.md` / `.github/copilot-instructions.md`) — project context documented for AI tools (Claude, Copilot, Cursor)
- [ ] AI-powered feature inside the application (LLM integration) — exact scope TBD
- [ ] Public deployment (Fly.io/Render/Azure free tier) — live Swagger UI
- [ ] Problem Details (RFC 7807) — standardized error responses instead of raw stack traces
- [ ] Health checks (`/health`) — Postgres/RabbitMQ liveness, wired into Docker healthcheck
- [ ] ADRs (`docs/adr/`) — technical decisions formalized as numbered Architecture Decision Records
- [ ] Visual README — embedded architecture diagram (Mermaid), Swagger screenshots

## Technical decisions

### CQRS

Logical separation between writes and reads, same database (Postgres) for both:

- **Command side**: tracked `DbContext`, regular `SaveChanges`.
- **Query side**: `AsNoTracking()` + projection straight to a DTO, no tracking overhead.

**Natural next step**: physically split the write database (normalized) from the read database (denormalized, query-optimized), kept in sync via an async event published to the queue on every state change. Pairs well with Event Sourcing. Out of scope here given the operational cost of running two databases in sync, but it's the natural evolution if the domain ever needed read scale far beyond its write volume.

### Why Wolverine instead of MediatR/MassTransit

MediatR and MassTransit both moved to a commercial licensing model. Wolverine covers CQRS and messaging in a single open-source framework, with official `net10.0` support.

### Why Mapster instead of AutoMapper

AutoMapper (same author as MediatR) also went commercial. Mapster is genuinely open source, source-generator based (no runtime reflection overhead), with convention-based mapping and explicit `IRegister` configuration when property names diverge.

### Why not ASP.NET Core Identity

Too much scaffolding (its own tables, cookie-based by default) for what this project needs. JWT Bearer + Keycloak demonstrates real OAuth2/OIDC, closer to what production systems actually use.

## Running it (current state)

```bash
docker compose up -d          # starts Postgres
dotnet ef database update --project src/OrderFlow.Infrastructure --startup-project src/OrderFlow.Api
dotnet run --project src/OrderFlow.Api
```

Full setup instructions (queue, observability, etc.) will be added as each piece ships.
