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

Asynchronous flow, triggered by Application after an order is created:

```
┌────────────────┐
│  Application   │
└────────────────┘   publishes OrderCreated
        │
        ▼
┌────────────────┐
│    RabbitMQ    │
└────────────────┘   same process also consumes
        │
        ▼
┌────────────────┐
│     Redis      │
└────────────────┘   order-by-status cache, refreshed
                      by the consumer, no TTL
```

`OrderStatusChanged` (triggered by confirming/cancelling an order) is planned next — it will extend this same flow to keep both the old and new status caches in sync.

## Stack

- **.NET 10** / C#
- **PostgreSQL** via Docker Compose
- **EF Core 10** + Npgsql, explicit mapping via Fluent API (`IEntityTypeConfiguration`)
- **Wolverine** — CQRS (Command/Query handlers) and messaging in a single open-source framework
- **Mapster** — DTO ↔ Command/Domain mapping, convention-based and explicit (`IRegister` + `Scan`)
- **RabbitMQ** — async queue, message-driven cache refresh
- **Redis** — read cache for orders by status, kept fresh by the queue consumer (no TTL, event-driven only)
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
- [x] Async queue (RabbitMQ via Wolverine's transport) — `OrderCreated` published on order creation, consumed by the same process
- [x] Redis read cache — `GetOrdersByStatus` refreshed by the queue consumer, no TTL (demonstrates the pattern; the project's data volume doesn't call for a real performance win)

### Planned

- [ ] `ConfirmOrder`/`CancelOrder` commands + `OrderStatusChanged` event — extends the cache refresh to status transitions, not just creation
- [ ] Global exception handling middleware (`IExceptionHandler`) + Problem Details (RFC 7807) — standardized error responses instead of raw stack traces
- [ ] Observability — OpenTelemetry (traces, metrics, logs) + dashboard (Grafana or Seq, TBD)
- [ ] Resilience — Polly (retry/circuit breaker) on the queue consumer
- [ ] Authentication — JWT Bearer + Keycloak (not ASP.NET Core Identity — see technical decisions below)
- [ ] Integration tests with Testcontainers (real Postgres/RabbitMQ, no mocks)
- [ ] CI — GitHub Actions (build, test, publish image)
- [ ] Full containerization via Docker Compose (API + infra in a single `up`)
- [ ] AI assistant configuration in the repo (`CLAUDE.md` / `.github/copilot-instructions.md`) — project context documented for AI tools (Claude, Copilot, Cursor)
- [ ] AI-powered feature inside the application (LLM integration) — exact scope TBD
- [ ] Public deployment (Fly.io/Render/Azure free tier) — live interactive API docs
- [ ] Health checks (`/health`) — Postgres/RabbitMQ liveness, wired into Docker healthcheck
- [ ] ADRs (`docs/adr/`) — technical decisions formalized as numbered Architecture Decision Records
- [ ] Visual README — embedded architecture diagram (Mermaid), API docs screenshots
- [ ] Dedicated Worker Service for the RabbitMQ consumer — decouples API scaling (request volume) from consumer scaling (processing volume), the competing-consumers pattern
- [ ] Horizontal scale-out demo — multiple Api replicas behind a load balancer (nginx/YARP) in Docker Compose, proving statelessness in practice, not just claiming it

## Technical decisions

### CQRS

Logical separation between writes and reads, same database (Postgres) for both:

- **Command side**: tracked `DbContext`, regular `SaveChanges`.
- **Query side**: `AsNoTracking()` + projection straight to a DTO, no tracking overhead.

**Natural next step**: physically split the write database (normalized) from the read database (denormalized, query-optimized), kept in sync via an async event published to the queue on every state change. Pairs well with Event Sourcing. Out of scope here given the operational cost of running two databases in sync, but it's the natural evolution if the domain ever needed read scale far beyond its write volume.

### Why the Redis cache has no TTL

The `orders:status:{status}` cache is fully event-driven: the queue consumer refreshes it on every `OrderCreated` event, so there's no reason to expire it on a timer — a TTL would just force an unnecessary Postgres read even when nothing changed. Trade-off: if an event were ever lost or failed silently, the cache would stay stale indefinitely with no self-healing. Acceptable here because the queue is the only writer and failures are visible in the RabbitMQ dead-letter queue.

### Why Wolverine instead of MediatR/MassTransit

MediatR and MassTransit both moved to a commercial licensing model. Wolverine covers CQRS and messaging in a single open-source framework, with official `net10.0` support.

### Why Mapster instead of AutoMapper

AutoMapper (same author as MediatR) also went commercial. Mapster is genuinely open source, source-generator based (no runtime reflection overhead), with convention-based mapping and explicit `IRegister` configuration when property names diverge.

### Why not ASP.NET Core Identity

Too much scaffolding (its own tables, cookie-based by default) for what this project needs. JWT Bearer + Keycloak demonstrates real OAuth2/OIDC, closer to what production systems actually use.

## Running it (current state)

```bash
docker compose up -d          # starts Postgres, RabbitMQ, Redis, RedisInsight
dotnet ef database update --project src/OrderFlow.Infrastructure --startup-project src/OrderFlow.Api
dotnet run --project src/OrderFlow.Api
```

Once it's up:

- API: `https://localhost:7048` (raw OpenAPI spec at `/openapi/v1.json`; interactive docs via Scalar still on the roadmap)
- RabbitMQ management UI: `http://localhost:15672` (`guest`/`guest@123`)
- RedisInsight: `http://localhost:5540` (add the `redis:6379` database on first run — use the service name `redis`, not `localhost`, since it runs inside the same Docker network)

Full setup instructions (observability, etc.) will be added as each remaining piece ships.
