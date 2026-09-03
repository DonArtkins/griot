# Code Standards

## Contracts Synchronization Gate

**CRITICAL:** Any implementation change that corrects or changes a contract must be reflected in the same branch across the affected feature spec, dependent future specs, relevant context files, root `AGENTS.md`, and `progress-tracker.md`. Contracts include EF Core entities/relations and enum values, REST route signatures, GraphQL type/query/mutation names, auth token claims and endpoints, `GRIOT_SERVICE_TOKEN` behavior, env variables, Docker/Compose service names and ports, storage paths, generated file structure, package versions, and file ownership.

**VERIFICATION GATE:** Before any commit, push, or PR:
- `dotnet build` + `dotnet test` (backend)
- `npm run lint`, `npm run typecheck`, `npm test`, `npm run build` (each Node project with its own lockfile)
- `flutter test` (when `mobile/` touched)
- Newman + Cypress where applicable (CI-enforced)
- `docker compose up` local parity check for backend changes

A feature is not ready for review while later specs or context still describe stale contracts.

## General

- The research corpus (`research/`) and the bootcamp PDF are the authority on *what*; this kit is the authority on *how*.
- One concern per project/class/function. No God classes, no logic in controllers, no SQL in services.
- Prefer explicit types and validated boundaries over implicit assumptions.
- Fix root causes rather than layering workarounds.
- No AI slope: comments explain *why*, never *what*; no decorative narration, no marketing-speak, no celebratory language.
- Keep the isolation contract: `gtp-*` names, non-default ports, per-repo version pins, per-project secrets.

## C# / .NET (backend/)

- Target `net8.0`, C# 12, nullable enabled, `ImplicitUsings` enabled.
- `Griot.Domain` has zero project references; enums live here (TaskStatus, Priority, Role, NotificationType).
- `Griot.Application` services depend on interfaces defined in `Griot.Domain` (or a ports/interfaces area); inject via constructor.
- `Griot.Infrastructure`: EF Core `GriotDbContext` maps entities in `OnModelCreating` (no reverse-engineering). Dapper repos sit beside EF repos, used only for the documented raw-SQL/stored-proc hot paths.
- Never pass SQL strings built by string concatenation to Dapper; always parameterize.
- Async everywhere: `Task<T>`, `ConfigureAwait(false)` in libraries.
- Structured logging via `ILogger<T>`; no `Console.WriteLine` in the API.
- Controllers: thin. Return `ActionResult<T>`; validation via `[ApiController]` + data annotations; map to DTOs — never expose entities directly.
- HotChocolate: resolvers call services only; use `DataLoader<,>` for N+1-prone children (assignees, comments).
- Migrations are committed to `src/Griot.Infrastructure/Migrations/`; procs live in `src/Griot.Infrastructure/Sql/`.

## TypeScript / React (web/)

- TypeScript strict; no `any`; explicit interfaces for API contracts; shared types co-located near features.
- Vite + React 18; function components; hooks first.
- Server data: Apollo for GraphQL reads, TanStack Query for REST reads/mutations — never mirrored into Zustand.
- Zustand holds client-only state only (filters, modal open/close, drag state, auth access token in memory).
- Auth: access token in memory only; refresh token in `httpOnly; Secure; SameSite=Lax` cookie; no `localStorage` tokens.
- Public shell is the only place GSAP/Lenis touches; boot via dynamic import; use `useGSAP()` (StrictMode-safe).
- MUI theme sourced from `ui-tokens.md` — no hardcoded colors outside the token file.

## Dart / Flutter (mobile/)

- Feature-first folders mirror the backend module split (`features/auth|dashboard|boards|tasks|notifications`).
- Riverpod for state; dio for REST with the same 401→refresh→retry interceptor pattern as web Axios.
- Access token in memory; refresh token in `flutter_secure_storage`; silent refresh on boot.
- Status change is a picker, not drag-drop (mobile idiom).
- `flutter analyze` clean; widget tests for key widgets.

## SQL Server / T-SQL

- Tables/columns: PascalCase entities map to T-SQL `dbo` schema; use `nvarchar(max)` for bodies, `UNIQUEIDENTIFIER` for PKs, `SYSUTCDATETIME()` for timestamps.
- Indexes: index every FK; composite indexes for the query shapes in the ERD; partial/filtered indexes where SQL Server allows.
- Stored procs: `usp_` prefix; parameterized; set `NOCOUNT ON`; single round-trip for dashboard summary.
## Auth & Security

- Argon2 for password hashing; never plaintext, SHA, or MD5.
- JWT access tokens: 15-min TTL, claims `sub`/`wid`, env-provided signing key; reject on invalid `iss`/`aud`.
- Refresh tokens: opaque, hashed at rest, single-use rotation, revoke-on-reuse (replay must fail).
- Redis sliding-window rate limiting on login + query-cost guard on `/graphql`.
- CORS allow-list only: Vercel origin in prod, localhost in dev.
- `GRIOT_SERVICE_TOKEN` → `ai-agent` principal with reduced role; HMAC verify `/api/webhooks/trigger`.
- Prompt injection: user text is data, never instructions; tools apply workspace/entity scoping.

## Tests and Verification

- Test harness is mandatory and baked in from the first feature of each app.
- Backend: xUnit + `WebApplicationFactory<Program>` against the real SQL Server container; cover the refresh-rotation replay race.
- Web: Jest + React Testing Library; mocks for Apollo/React Query hooks; assert behavior, minimal snapshots.
- Mobile: Flutter widget + integration tests.
- API contract: Postman collection rerun as Newman in CI with `pm.response.to.have.jsonSchema` checks.
- AI layer: golden-transcript tests with a mocked LLM client; MCP tool contract tests; Cypress uses an MSW-stubbed copilot (no LLM in CI).
- Coverage gate ≥80% line coverage, service-layer and auth emphasized (vet aggregate flat rates).

## Feature Spec Methodology

- One spec = one feature; `Type` is `NEW FEATURE` (or `MODIFICATION` naming the affected spec).
- Every spec must include **Setup / Initialization** (exact scaffold commands), **Separation of Concerns**, and **Docker & Deploy** sections.
- Dependencies are exact and must exist before implementation. Files are `CREATE` / `MODIFY` / `RUN`.
- `Out of Scope` is mandatory; Acceptance Criteria are binary pass/fail.
- Setup commands (scaffold/create/init) belong in the spec that creates the app — never assumed done.
- Never put project-level initialization in later feature specs.

## Quality Gate

Every deliverable passes three checks before it counts as complete:
- **Documents**: consistent, no placeholders, contracts synchronized, actionable.
- **Code**: builds, tests green, coverage met, no secrets in code, structured logging, CI green.
- **Research/Intelligence**: stack decisions traceable to the bootcamp PDF or a marked `[own-stack]` rationale.