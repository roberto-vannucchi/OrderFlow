# OrderFlow

Projeto de portfólio em .NET — domínio simples (pedidos), stack rica: CQRS, fila, observability.

## Decisões técnicas

### CQRS

Separação lógica entre escrita e leitura, mesmo banco (Postgres) para os dois:

- **Command side**: `DbContext` rastreado, `SaveChanges` normal.
- **Query side**: `AsNoTracking()` + projeção direto para DTO, sem overhead de tracking.

**Evolução natural**: separar fisicamente o banco de escrita (normalizado) do banco de leitura (denormalizado, otimizado para consulta), sincronizados via evento assíncrono publicado na fila a cada mudança de estado. Combina bem com Event Sourcing. Fora do escopo deste projeto por complexidade operacional (dois bancos + sincronização), mas é o próximo passo natural caso o domínio crescesse para exigir escala de leitura muito maior que a de escrita.
