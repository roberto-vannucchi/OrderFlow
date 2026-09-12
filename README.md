# OrderFlow

Projeto de portfólio em .NET — domínio simples (pedidos), stack rica em tecnologia: CQRS, mensageria, observability.

## Stack

- **.NET 10** / C#
- **PostgreSQL** via Docker Compose
- **EF Core 10** + Npgsql, mapping explícito via Fluent API (`IEntityTypeConfiguration`)
- **Wolverine** — CQRS (Command/Query handlers) e mensageria no mesmo framework, open source
- **Mapster** — mapping DTO ↔ Command/Domain, por convenção e explícito (`IRegister` + `Scan`)
- **RabbitMQ** (planejado) — fila assíncrona
- **OpenTelemetry** (planejado) — tracing/metrics/logs
- **Testcontainers** (planejado) — testes de integração contra Postgres/RabbitMQ reais

## Roadmap

### Concluído

- [x] Estrutura da solution (`.slnx`, `src/Api`, `src/Application`, `src/Infrastructure`)
- [x] Domínio `Order` — modelo rico (setters privados, factory method, regras de negócio via método)
- [x] Mapping EF Core explícito (nome de tabela/coluna, precisão decimal, conversão de enum, índice)
- [x] Postgres via `docker-compose.yml` com volume persistente
- [x] Primeira migration aplicada, round-trip validado ponta a ponta (endpoint temporário)
- [x] Repositório Git + GitHub configurados
- [x] CQRS com Wolverine — `CreateOrder` (Command) e `GetOrder` (Query) reais, substituindo os endpoints temporários
- [x] Mapper DTO ↔ Command/Domain com Mapster — mapeamento por convenção e explícito (`OrderReference` calculado via `IRegister`)

### Planejado

- [ ] Fila assíncrona (RabbitMQ via transporte do Wolverine) — evento publicado a cada mudança de estado do pedido
- [ ] Observability — OpenTelemetry (traces, metrics, logs) + dashboard (Grafana/Seq, a definir)
- [ ] Resiliência — Polly (retry/circuit breaker) no consumer da fila
- [ ] Autenticação — JWT Bearer + Keycloak (não ASP.NET Core Identity — ver decisão técnica abaixo)
- [ ] Testes de integração com Testcontainers (Postgres/RabbitMQ reais, sem mock)
- [ ] Documentação de API — OpenAPI + Scalar
- [ ] CI — GitHub Actions (build, test, publish de imagem)
- [ ] Containerização completa via Docker Compose (API + infra num único `up`)
- [ ] Configuração de assistente de IA no repositório (`CLAUDE.md` / `.github/copilot-instructions.md`) — contexto do projeto documentado para ferramentas de IA (Claude, Copilot, Cursor)
- [ ] Feature de IA dentro da aplicação (integração com LLM) — escopo exato a definir

## Decisões técnicas

### CQRS

Separação lógica entre escrita e leitura, mesmo banco (Postgres) para os dois:

- **Command side**: `DbContext` rastreado, `SaveChanges` normal.
- **Query side**: `AsNoTracking()` + projeção direto para DTO, sem overhead de tracking.

**Evolução natural**: separar fisicamente o banco de escrita (normalizado) do banco de leitura (denormalizado, otimizado para consulta), sincronizados via evento assíncrono publicado na fila a cada mudança de estado. Combina bem com Event Sourcing. Fora do escopo deste projeto por complexidade operacional (dois bancos + sincronização), mas é o próximo passo natural caso o domínio crescesse para exigir escala de leitura muito maior que a de escrita.

### Por que Wolverine em vez de MediatR/MassTransit

MediatR e MassTransit passaram para modelo de licenciamento comercial. Wolverine cobre CQRS e mensageria no mesmo framework, open source, com suporte oficial a `net10.0`.

### Por que Mapster em vez de AutoMapper

AutoMapper (mesmo autor do MediatR) também passou para modelo de licenciamento comercial. Mapster é open source, baseado em source generator (sem overhead de reflection em runtime), com mapeamento por convenção e configuração explícita via `IRegister` quando os nomes de propriedade divergem.

### Por que não ASP.NET Core Identity

Scaffold pesado (tabelas próprias, cookie-based por padrão) para o que o projeto precisa. Autenticação via JWT Bearer + Keycloak demonstra OAuth2/OIDC real, mais alinhado com o que se vê em produção.

## Como rodar (estado atual)

```bash
docker compose up -d          # sobe o Postgres
dotnet ef database update --project src/OrderFlow.Infrastructure --startup-project src/OrderFlow.Api
dotnet run --project src/OrderFlow.Api
```

Instruções completas de setup (fila, observability, etc.) serão adicionadas conforme cada peça for implementada.
