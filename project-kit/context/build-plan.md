# Build Plan

## Contracts Synchronization Gate

Any implementation change that corrects or changes a contract must be reflected in the same branch across the affected feature spec, dependent future specs, relevant context files, root `AGENTS.md`, and `progress-tracker.md`. A feature is not ready for review while later specs or context still describe stale fields, old API shapes, or invalid contracts.

## Purpose

This document defines the phase-by-phase implementation plan for Project Griot across the seven bootcamp weeks. It provides clear sequencing, done criteria, and dependencies for each build phase, mapped to the `feature-specs/` files.

## Build Approach

Griot follows a **design-first, front-end-aware, stack-exact** methodology:

1. **Design in Figma first** — screens (Week 1) → ERD in FigJam (Week 2) → everything else is transcription.
2. **Infrastructure parity early** — `docker compose` brings up the full data stack before any app code exists.
3. **Backend API before clients** — web and mobile both consume the same REST/GraphQL surface.
4. **Separation of concerns enforced in the repo structure** — each app is an isolated folder/package.
5. **Deploy each app as soon as its first working slice exists** — never at the end.

## Phase 0: Repository Skeleton (Feature 01)

**Objective:** Monorepo layout, git hygiene, separation-of-concerns scaffolding, and root tooling so everything that follows drops into its own folder.

**Done Criteria:**
- [ ] `griot/` monorepo with `backend/`, `web/`, `mobile/`, `ai/`, `mcp/` placeholders and READMEs
- [ ] Root `.gitignore` (env, node_modules, build artifacts) and `.env.example`
- [ ] Root `docker-compose.yml` defined (even if not started yet)
- [ ] `PROMPTS/` and `project-kit/` committed (this kit)
- [ ] GitHub repo configured; branch protection on `main`

**Key Files:** root `.gitignore`, `.env.example`, `README.md`, `project-kit/**`

**Dependencies:** None (foundational)

---

## Phase 1: Development Infrastructure (Feature 02)

**Objective:** Real Docker Engine + Compose v2 on Parrot with the whole bootcamp data stack running and verified.

**Done Criteria:**
- [ ] `gtp-sqlserver`, `gtp-postgres`, `gtp-redis` containers up on ports 14333/5433/6380
- [ ] SQL Server reachable from DBeaver; `SELECT @@VERSION` works
- [ ] Redis `PING` → PONG; Postgres accepts connections
- [ ] `.env` holds `GTP_SA_PASSWORD` / `GTP_PG_PASSWORD` (git-ignored)

**Key Files:** root `docker-compose.yml`, `.env.example`, `research/gtp-2026-prep.md`

**Dependencies:** Phase 0

---

## Phase 2: Database Schema (Feature 03)

**Objective:** The FigJam ERD (Week 2 deliverable, `project-kit/diagrams/erd/`) transcribed into the EF Core `GriotDbContext`, migrated to SQL Server, with Dapper stored procedures.

**Done Criteria:**
- [ ] ERD approved in FigJam and exported PNG stored in `project-kit/diagrams/erd/`
- [ ] All v1 entities + enums in `Griot.Domain`; `GriotDbContext` maps them in `OnModelCreating`
- [ ] `InitialCreate` migration applies to SQL Server 2022
- [ ] `usp_BulkUpdateTaskStatus` + `usp_GetDashboardSummary` in `src/Griot.Infrastructure/Sql/`
- [ ] Indexes on foreign keys and hot query paths (per ERD)

**Key Files:** `backend/src/Griot.Domain/**`, `backend/src/Griot.Infrastructure/**`, `project-kit/diagrams/erd/`

**Dependencies:** Phase 1, Feature 01

---

## Phase 3: Backend API — REST + GraphQL + Auth (Feature 04)

**Objective:** The ASP.NET Core 8 Web API with controllers, HotChocolate, the `Griot.Application` service layer, and owned auth (Argon2 + JWT + rotated refresh tokens + Redis rate limits).

**Done Criteria:**
- [ ] `Griot.sln` builds clean; `dotnet build` passes
- [ ] Service layer + REST endpoints for all six modules (workspace, project, board, task, comment, notification)
- [ ] HotChocolate schema + queries/mutations/DataLoaders live at `/graphql`
- [ ] Auth endpoints (register/login/refresh/logout) with refresh rotation
- [ ] `GRIOT_SERVICE_TOKEN` resolves to the restricted `ai-agent` principal
- [ ] Postman collection (REST + GraphQL) saved for Newman
- [ ] `docker compose up` runs api + sqlserver + postgres + redis

**Key Files:** `backend/src/Griot.Api/**`, `backend/src/Griot.Application/**`, Postman collection

**Dependencies:** Phase 2

## Phase 4: Web Frontend (Feature 05)

**Objective:** Vite + React 18 + MUI App with the two shells, Apollo + TanStack data layer, Zustand client state, and auth against the .NET API.

**Done Criteria:**
- [ ] MUI theme derived from Week-1 tokens; no default-purple surfaces
- [ ] Public shell (landing/pricing/login with GSAP via dynamic import) + protected App shell
- [ ] Dashboard, board (drag-drop), task detail, team settings, notifications — all wired to REST/GraphQL
- [ ] Auth: access token in memory, refresh in `httpOnly` cookie, silent refresh on boot
- [ ] `npm run build` passes; Lighthouse run recorded

**Key Files:** `web/**`

**Dependencies:** Phase 3, ui-context/ui-tokens/ui-rules/ui-registry

---

## Phase 5: Mobile App (Feature 06)

**Objective:** Flutter app consuming the same GraphQL endpoint via `graphql_flutter`, REST via dio, Riverpod state, secure token storage.

**Done Criteria:**
- [ ] `flutter create griot_mobile`; login/signup against the .NET API
- [ ] Dashboard + boards via GraphQL; task detail + status picker + notifications
- [ ] Access token in memory, refresh in `flutter_secure_storage`, silent refresh on boot
- [ ] `flutter doctor` clean for Android; verified on emulator + one physical device

**Key Files:** `mobile/**`

**Dependencies:** Phase 3, Feature 04

---

## Phase 6: AI Layer (Feature 07)

**Objective:** Trigger.dev v3 agents, MCP server, and Copilot panel — all wrapped around the API with the service-token boundary.

**Done Criteria:**
- [ ] `ai/` project connected; `griotCopilot` agent answers from board data
- [ ] Scheduled: `dueReminders`, `sprintDigest`, `staleBoard` running in dev
- [ ] Web Copilot panel streams responses; approve-before-write mutations work
- [ ] `mcp/` server runs stdio + Streamable HTTP; `get_board`/`create_task` verified in an MCP client
- [ ] Cost caps + token budget enforced; golden-transcript tests green

**Key Files:** `ai/**`, `mcp/**`, web Copilot panel

**Dependencies:** Phase 4

---

## Phase 7: CI/CD + Deployment (Features 08–09)

**Objective:** GitHub Actions gates + automated deploy of every app (web → Vercel, backend → Railway, mcp → Railway, ai → Trigger cloud).

**Done Criteria:**
- [ ] CI job: `dotnet build/test`, `npm run lint/typecheck/test/build`, `flutter test`, Newman, Cypress
- [ ] Backend Dockerfile multi-stage; compose v2 local parity
- [ ] Web live on Vercel (Vite preset), talking to the deployed API
- [ ] Backend live on Railway; REST + GraphQL responding; migrations as release command
- [ ] MCP live as Streamable HTTP; AI agents scheduled in Trigger cloud
- [ ] Mobile APK built in CI and downloadable

**Key Files:** `.github/workflows/`, backend `Dockerfile`, `docker-compose.yml`

**Dependencies:** Phases 3–6

---

## Phase 8: Quality Engineering (Feature 10)

**Objective:** The full Week-6/7 gate — unit/integration, E2E, API contract, performance, security, accessibility, coverage ≥80%.

**Done Criteria:**
- [ ] xUnit suites (incl. refresh-rotation replay), Jest+RTL, Flutter widget/integration tests
- [ ] Cypress core-loop suite green; Newman collection green in CI
- [ ] k6 baseline recorded; OWASP review logged; axe pass on Public shell
- [ ] Manual cycle + UAT + executive test summary completed (Week 7)

**Key Files:** `backend/tests/**`, `web` test folders, `mobile/test/**`, `k6/**`

**Dependencies:** Phases 3–7

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**