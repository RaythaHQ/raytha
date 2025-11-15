# Raytha CMS — Copilot Instructions

applyTo:
  - "src/**"
  - "Raytha.sln"
  - "tests/**"

## Goal
Produce production-ready contributions to **Raytha CMS**: a .NET 8, clean-architecture, multi-project solution. Favor correctness, security, maintainability, and minimal dependencies.

## Tech Stack & Conventions
- **Runtime/Framework:** .NET 8 (ASP.NET Core with Razor Pages where applicable)
- **Architecture:** Clean architecture
  - `Raytha.Domain` — entities, value objects, domain events, no outward deps
  - `Raytha.Application` — CQRS (MediatR-style), DTOs, validators
  - `Raytha.Infrastructure` — EF Core, storage providers (local, R2, Azure), auth integrations
  - `Raytha.Migrations.Postgres` — EF Core migrations only Postgres
  - `Raytha.Migrations.SqlServer` — EF Core migrations only for SQL Server
  - `Raytha.Web` — endpoints/controllers, minimal views/JS, no business logic
- **Language:** C# 12, nullable enabled
- **Async:** `async/await` end-to-end; no blocking on async (`.Result`, `.Wait()` are disallowed)
- **Logging:** `ILogger<T>`; structured logs; no Console.WriteLine
- **Configuration:** strongly typed options via `IOptions<T>`; never read env vars directly outside startup
- **Validation:** FluentValidation in `Application`; controllers/endpoints assume validated inputs
- **Mapping:** Not using an auto-mapper, but manual mapping methods/extensions
- **AuthN/Z:** ASP.NET Core Identity/Authorization handlers/policies; no role checks scattered in UI
- **HTTP:** Use `HttpClientFactory`; set sensible timeouts
- **Serialization:** `System.Text.Json`; camelCase; case-insensitive read; don’t change global options casually
- **Database:** EF Core with migrations; no raw SQL unless truly necessary; repository pattern only if already present
- **Testing:** nunit; deterministic tests; avoid test flakiness

## Pull Request Expectations
When you create or update a PR:
1. **Explain the change** (what/why), list affected projects and public APIs.
2. **Risk & rollback**: note risks, DB migrations, and rollback plan.
3. **Validation**: include test coverage summary and manual steps.
4. **Performance**: call out hot paths or allocations if relevant.

## Coding Standards (do)
- Keep razor pages/controllers/endpoints thin; push logic into `Application` layer.
- Prefer **records** for immutable models and responses.
- Use **guard clauses**; return early on invalid state.
- Add **XML doc comments** for public APIs; summary + param + returns.
- Use **CancellationToken** on all async methods; plumb it through.
- Check `ModelState.IsValid` only where model binding occurs; otherwise validate in `Application`.
- For I/O: add **timeouts**, **retries** (Polly-style) where appropriate, and **idempotency** for commands.
- Feature flags for risky changes.
- Add **Telemetry** events (Activity/OTel) for notable operations.

## Coding Standards (don’t)
- Don’t perform data access in controllers.
- Don’t expose EF entities over the wire—return DTO/view models.
- Don’t swallow exceptions; use typed results or problem details.
- Don’t add new static singletons; prefer DI.
- Don’t introduce new dependencies without justification.

## API/Endpoints
- Return **ProblemDetails** for errors; avoid leaking stack traces.
- Use **RFC 7807** semantics consistently.
- Pagination: `page`, `pageSize` (default 20, max 200). Return `X-Total-Count` when feasible.
- Ids: use **GUID** or existing key conventions; validate format at the boundary.

## Security
- Validate all inputs (lengths, enums, regex where needed).
- Use parameterized queries (EF Core handles this).
- Never log secrets or PII.
- Enforce authorization policies at the handler level for commands/queries that need it.
- Add **anti-forgery** on form posts; for APIs, require proper auth tokens.
- Sanitize HTML if user-generated content is rendered.

## Performance
- Be allocation-aware in hot paths; prefer `AsNoTracking()` for read-only queries.
- Use `Select` projections instead of materializing full entities.
- Index DB columns used in filters/sorts; include migration + script.
- Cache **read-mostly** configuration or metadata via IMemoryCache where appropriate, with sensible TTLs.

## UI/Frontend (if touched)
- Keep JS minimal and framework-agnostic unless project dictates otherwise.
- Accessibility: proper labels, roles, focus order; keyboard navigable.
- Localize user-facing strings; use established i18n resource files.

## Storage Providers
- Abstraction lives in `Application` interfaces; implementations in `Infrastructure` (Local Disk, R2, Azure Blob).
- All file writes are **atomic** and **streamed**; validate content type and size.
- Return provider-agnostic URLs or IDs via the abstraction.

## Observability
- Log: one line per event with context (userId, requestId, correlationId).
- Metrics: expose counters/timers for key operations (render, save, auth).
- Tracing: create Activities around external calls and DB operations.

## Testing Guidance
- Unit tests for domain logic and handlers (happy + failure paths).
- Integration tests for EF Core (Testcontainers or SQLite in-memory if appropriate).
- Web tests for critical endpoints (minimal, deterministic).
- For bugs/regressions: add a failing test first, then fix.

## Commit & Branching
- Conventional commits: `feat:`, `fix:`, `perf:`, `refactor:`, `test:`, `docs:`, `chore:`, `build:`.
- One logical concern per commit; small, reviewable PRs.

## How to Run Locally (baseline)
- `dotnet build Raytha.sln`
- `dotnet ef database update` (if `Infrastructure` has migrations)
- `dotnet run --project src/Raytha.Web`
- Default env: `ASPNETCORE_ENVIRONMENT=Development`
- Provide sample `appsettings.Development.json` if adding new settings.

## When Copilot Is Unsure
- Prefer suggesting **two minimal options** with trade-offs.
- Insert `// TODO(zack):` for decisions needing human input.
- If a change spans layers, propose the **smallest viable slice** and outline follow-ups.

## PR Checklist (Auto-include in PR body)
- [ ] Tests added/updated
- [ ] DI registrations updated
- [ ] Nullability satisfied
- [ ] CancellationToken plumbed
- [ ] Logging + telemetry added
- [ ] Migrations included (if needed)
- [ ] Docs/README updated (if needed)

