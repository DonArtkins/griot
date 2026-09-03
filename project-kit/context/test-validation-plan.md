# Test Validation Plan

## Contracts Synchronization Gate

Any implementation change that corrects or changes a contract must be reflected in the same branch across the affected feature spec, dependent future specs, relevant context files, root `AGENTS.md`, and `progress-tracker.md`. Contracts include EF Core entities/relations and enum values, REST route signatures, GraphQL type/query/mutation names, auth token claims and endpoints, Docker/Compose service names and ports, env variables, and file ownership.

## Objective

The bootcamp's Week 6–7 quality chain, built from the first feature onward. The gate: **≥80% line coverage (service-layer and auth emphasized), every suite green in CI, OWASP review logged, k6 baselines recorded, accessibility pass on the Public shell.**

## Test Pyramid

| Layer | Tool | Scope | Runs |
|---|---|---|---|
| Backend unit | xUnit | Services, domain rules, auth rotation logic (mocked repos) | `dotnet test` |
| Backend integration | xUnit + `WebApplicationFactory<Program>` | Auth flows (refresh rotation replay, rate limit), core CRUD vs real SQL Server container | `dotnet test` |
| Frontend | Jest + React Testing Library | TaskCard, BoardView, modals, state transitions; MSW for copilot stub | `npm test` |
| API contract | Newman (Postman collection) | REST + GraphQL; `pm.response.to.have.jsonSchema` checks | CI job |
| E2E | Cypress | Core loop: signup → project → tasks → drag to Done; stubbed copilot | CI job |
| Performance | k6 (`[own-stack]`) | Dashboard query, login, board read — p95 < 500 ms | Baseline recorded Week 6, regression Week 7 |
| Mobile | `flutter test` + `integration_test` | Widget + integration for the Week-4 app | `flutter test` |
| AI | Vitest golden transcripts | Tool-call order with a mocked LLM client; MCP tool JSON contracts | `npm test` (ai/, mcp/) |
| Accessibility | axe (RTL/Lighthouse) | Public + App shells | Week 6 gate |
| Security | OWASP review | Injection, broken auth, sensitive data, CORS | Week 6 gate, logged |

## Must-Cover Scenarios (non-negotiable)

1. **Refresh-token rotation replay**: login → capture refresh → reuse twice → second call must be 401/revoked. (The Week-6 "broken auth" OWASP line.)
2. **Bulk status update atomicity**: `usp_BulkUpdateTaskStatus` updates all-or-nothing via transaction.
3. **N+1 prevention**: dashboard/board queries do not issue N+1 GraphQL/EF calls (DataLoader + includes).
4. **401 → silent refresh → retry-once** in web axios and mobile dio interceptors.
5. **Copilot propose-before-write**: no mutation executed without user approval; MSW-stubbed in CI.
6. **`GRIOT_SERVICE_TOKEN` scope**: ai-agent principal cannot delete, invite, or read outside its workspace.
7. **Rate limit** on `/api/auth/login` and query-cost guard on `/graphql`.

## Environment Strategy

- **Local**: `docker compose up` → real SQL Server/Postgres/Redis; `dotnet run` + `npm run dev`.
- **CI**: GitHub Actions spins SQL Server 2022 service container; tests run against it. Postgres/Redis services added as tests need them.
- **Deployed**: manual + UAT + k6 regression against Railway/Vercel (Week 7) — env mismatches, CORS, cold starts are exactly what to catch there.

## Coverage & Quality Gate

- Enforce `--collect:"XPlat Code Coverage"` on `dotnet test`; collect thresholds in CI.
- Enforce coverage on **service-layer + auth first**, presentation second — a flat aggregate pass while auth is uncovered is a failure.
- Newman and Cypress jobs run **parallel** in CI and both gate merges on `main`.
- A feature is done only when its new logic has tests and the full gate above is green.

## Manual + UAT (Week 7)

- Full manual pass over both shells: auth, project/board/task CRUD, drag-drop, comments, notifications, bulk ops, settings/invites, Flutter app, Copilot chat + approve-mutation, MCP tools via Inspector.
- UAT: one person, one task — "Create a project, add three tasks, and move one to Done — no other instruction." Second persona: "Ask Griot what's blocked on your project" on the Copilot. Log hesitation and friction as findings even if not bugs.
- Every defect → Jira with repro steps, severity, and evidence; traceability matrix requirement ↔ test ↔ defect.

## Final Reporting (Week 7)

- Executive Test Summary (1–2 pages): verdict, pass/fail by area, top-3 risks, recommendation.
- k6 regression report vs Week-6 baseline.
- Test strategy doc is a **living document** started in Week 2, finalized in Week 7 — never a blank page.