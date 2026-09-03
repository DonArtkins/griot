# AGENTS.md — Project Griot (GTP 2026 Bootcamp)

## Read This First

You are an AI coding agent working on **Griot** (*GREE-oh*), a project-management web app built for the **Sababisha Solutions GTP 2026 Bootcamp**. Griot's product story: the system that keeps the team's "accurate, shared record of what happened and what's next" — workspaces, projects, boards, tasks, comments, notifications, and an AI copilot that summarizes and acts on that record.

The bootcamp defines the stack; Griot runs exactly on it. **The `GTP 2026 BOOTCAMP EDITION.pdf` "2026 Core Technology Stack" is the contract.** Every technology choice maps 1:1 to that guide with exactly two statuses: `exact` (we simply use it) or `[own-stack]` (the guide is silent — personal choice, always flagged). Never substitute the bootcamp's backend/frontend/mobile/devops platforms without a marked `[own-stack]` entry and a recorded decision.

The bootcamp's source of truth is the research corpus under `research/`. The implementation source of truth is this repo's `project-kit/`.

## Mandatory Reading Order

1. `research/gtp-2026-prep.md` — master strategy doc: the full stack (mapped to the PDF), environment setup, the isolation contract, and the seven-week index.
2. `research/week-01-fundamentals-and-system-design.md` … `research/week-07-real-world-qe-practice.md` — the per-week deltas; read the week(s) relevant to the current feature.
3. `research/ai-integration.md` — the AI agent + MCP layer (`[own-stack]` extension that wraps the bootcamp stack).
4. `project-kit/context/project-overview.md` — product definition, users, v1 scope, the two-shell design, success criteria.
5. `project-kit/context/architecture-context.md` — monorepo layout, separation of concerns, databases, APIs, auth, AI boundary, invariants.
6. `project-kit/context/build-plan.md` — phase-by-phase implementation plan with done criteria.
7. `project-kit/context/code-standards.md` — Griot-specific standards (extends the bootcamp + isolation rules).
8. `project-kit/context/library-docs.md` — per-library usage patterns and integration rules (EF Core, Dapper, HotChocolate, MUI, Apollo, Trigger.dev, MCP).
9. `project-kit/context/ui-context.md` — the design-system foundation and the two UI shells.
10. `project-kit/context/ui-tokens.md` — design tokens (colors, typography, spacing, radius, motion) mapped to the MUI theme and Flutter theme.
11. `project-kit/context/ui-rules.md` — UI behavior patterns, layout rules, component interaction standards.
12. `project-kit/context/ui-registry.md` — component library and usage patterns per surface.
13. `project-kit/context/ai-workflow-rules.md` — how agents work on this repo, planning gates, verification gates, contract synchronization.
14. `project-kit/context/test-validation-plan.md` — the Week-6/7 quality chain (xUnit, Jest+RTL, Cypress, Newman, k6, Flutter tests, ≥80% coverage).
15. `project-kit/context/progress-tracker.md` — current state, next steps, open questions, session notes.
16. `project-kit/diagrams/README.md` — the Figma/FigJam reference diagrams (ERD, architecture, wireframes) that govern implementation.

## The Tech Stack Contract (from the PDF, mapped 1:1)

| Layer | Bootcamp item | Status | Griot use |
|---|---|---|---|
| Backend | .NET 8 (LTS) + ASP.NET Core Web API | exact | Controllers (REST) + HotChocolate (GraphQL) in one process |
| ORM | Entity Framework Core 8 | exact | Code-first migrations to SQL Server + LINQ |
| Data | Dapper 2.x | exact | Stored-procedure / raw-SQL hot paths (bulk ops, dashboard) |
| GraphQL | HotChocolate GraphQL 14+ | exact | `/graphql` beside `/api`; Week-3 Apollo + Week-4 graphql_flutter consume it |
| Database | SQL Server 2022 (primary), PostgreSQL 16 (secondary) | exact | SQL Server in an official Linux container; Postgres for cohort/secondary exercises |
| Frontend | React 18.3, Vite 5, Material UI v6, Apollo, Axios, TanStack Query 5 | exact | Vite SPA, MUI re-themed from Week-1 Figma tokens |
| Mobile | Flutter 3.19+ / Dart 3, GraphQL Flutter, Riverpod | exact | Android APK (iOS out of scope — no Xcode on Linux) |
| DevOps | Docker 26+, Compose v2, Vercel, GitHub Actions, Railway | exact | Compose local stack, Vite preset on Vercel, PR-gated CI/CD |
| Auth | (guide silent) | `[own-stack]` | Custom JWT access/refresh (Argon2 + Redis), no vendor |
| State / routing / motion | (guide silent) | `[own-stack]` | Zustand (client state), React Router, GSAP+Lenis (Public shell only) |
| Perf / load | (guide silent) | `[own-stack]` | k6 |
| AI layer | (guide silent) | `[own-stack]` | Trigger.dev v3 agents, Griot MCP server, in-app Copilot |

## Monorepo Layout & Separation of Concerns

```
griot/
├── backend/                     # .NET 8 — single .sln, modular-but-pragmatic
│   ├── src/
│   │   ├── Griot.Api/           # entry point: Program.cs, controllers, GraphQL, middleware
│   │   ├── Griot.Application/   # services only (taskService, workspaceService, authService…)
│   │   ├── Griot.Domain/        # entities + enums; zero dependencies
│   │   └── Griot.Infrastructure/ # GriotDbContext (EF Core), repos (Dapper), Redis, migrations
│   └── tests/Griot.Tests/       # xUnit + WebApplicationFactory
├── web/                         # Vite + React 18 + MUI (two shells: Public + App)
├── mobile/                      # Flutter app (companion surface)
├── ai/                          # Trigger.dev v3 agents + workflows (Node 20, own lockfile)
├── mcp/                         # Griot MCP server (@modelcontextprotocol/sdk, own lockfile)
├── docker-compose.yml           # api + sqlserver + postgres + redis (local parity)
├── .github/workflows/           # CI: test gate → deploy
├── research/                    # bootcamp research (source of truth for the stack)
├── project-kit/                 # context/, feature-specs/, examples/, diagrams/
└── PROMPTS/                     # week-grouped Figma/figjam/agent prompts
```

**Rule:** one concern per project. `Griot.Application` never references EF Core; controllers never contain business logic; `web/` never talks to a database; `ai/` never holds database credentials. Each Node project is a separate npm package with its own `.nvmrc` (→ 20) and lockfile.

## The Isolation Contract (GTP stack vs. personal stack)

- GTP daemons are named `gtp-*` on non-default host ports (14333, 5433, 6380) and can never shadow personal Postgres/Redis/MySQL.
- Node is versioned via `nvm` per repo (`.nvmrc` → 20); .NET is pinned per repo via `global.json` (8.0) + `dotnet new tool-manifest`.
- Env/secrets are per-project; `.env` is git-ignored; `.env.example` documents the shape.
- The only shared resource is the Docker daemon; namespaces, networks and ports keep everything separate.

## Contracts Synchronization Gate

Any implementation change that corrects or changes a contract must be reflected, in the same branch, across the affected feature spec, dependent future specs, relevant context files, root `AGENTS.md`, and `progress-tracker.md`. Contracts include: EF Core entities/relations and enum values, REST route signatures, GraphQL type/query/mutation names, auth token claims and endpoints, `GRIOT_SERVICE_TOKEN` behavior, storage paths, generated file/directory structure, package versions, environment variables, Docker/Compose service names and ports, and file ownership. A feature is not ready for review while later specs or context still describe stale contracts.

## Verification Gates

Every feature is done only when:
- `dotnet build` is clean and `dotnet test` passes (xUnit, incl. the auth refresh-rotation case).
- `npm run build`, lint, typecheck and `npm test` pass for `web/`, `ai/`, and `mcp/`.
- `flutter test` passes when `mobile/` is touched.
- Newman (Postman collection) runs green in CI; Cypress core-loop suite green; coverage ≥80% on service-layer and auth.
- `docker compose up` brings the full local stack up and the API health-check answers.
- No hardcoded secrets; `.env.example` kept in sync; no `console.log` production logging.

## Docker & Deployment Rules (per app)

- **backend/**: multi-stage Dockerfile (`dotnet/sdk:8.0` build → `dotnet/aspnet:8.0` runtime), `EXPOSE 8080`, env-injected at runtime. Deployed to **Railway** (Docker-native; Render fallback; Azure App Service variant documented). EF migrations run as a Railway release command — never assumed from a local migration.
- **web/**: deployed to **Vercel** using the **Vite framework preset**; env `VITE_API_URL` points at the deployed API; static assets from `dist`. No custom server.
- **mobile/**: Android APK/AppBundle built in CI (Flutter); no container runtime needed — Docker is used to pin the Flutter build environment for reproducible releases.
- **ai/**: Trigger.dev v3 cloud (or self-hosted on Railway); scheduled + agent runs durable and observable from the Trigger dashboard.
- **mcp/**: Docker image running Streamable HTTP mode on Railway (symmetry with the API); stdio mode for local Claude/Cursor/Cline.
- Everything is composed locally via `docker-compose.yml` so deployment day is never the first time the wiring runs.

## Feature Spec Methodology

- One spec = one feature. Every spec carries `Type: NEW FEATURE` (or `MODIFICATION`), a `Setup / Initialization` section (exact scaffold commands for that app), a `Separation of Concerns` section, and a `Docker & Deploy` section.
- Dependencies must exist before implementation starts. Files are `CREATE` / `MODIFY` / `RUN`. `Out of Scope` is mandatory; `Acceptance Criteria` are binary pass/fail.
- Specs are implemented strictly in numeric order. Do not combine specs in one pass.
- The `project-kit/diagrams/` artifacts (ERD, C4/architecture, wireframes) govern the schema and feature specs — refer to the latest approved diagram before writing schema-related code.
- Setup commands must be present in the spec that creates a new app (scaffold, package install, env), never assumed already done.

## Weekly Focus Map

- **Week 1**: Fundamentals & system design — HackerRank, Parrot env, Figma Make design system, EF Core schema sketch.
- **Week 2**: Backend & API — DB schema design in Figma (this week's deliverable), SQL Server implementation, stored procs, REST + GraphQL.
- **Week 3**: Frontend — React/Vite/MUI, Apollo + Axios + TanStack, auth, two shells.
- **Week 4**: Mobile — Flutter + Riverpod + graphql_flutter.
- **Week 5**: Deployment & DevOps — Docker/Compose, Vercel, Railway, GitHub Actions.
- **Week 6**: Quality engineering — xUnit/Jest/Cypress/Newman/k6/OWASP, 80% gate.
- **Week 7**: Real-world QE — test strategy, manual + UAT cycles, exec summary, portfolio.

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
**Griot — the record of what the team built, and how well they built it.**