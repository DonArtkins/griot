# Griot — Project Management with an AI Copilot

**Griot** (*GREE-oh*) is a project-management web app for small teams — the "accurate, shared record of what happened and what's next". Workspaces → projects → boards → tasks, with comments, attachments, roles & invites, an activity feed, notifications — and an AI copilot that summarizes the board, drafts tasks, and acts on approval. It also features robust OTP 2FA authentication, comprehensive System Reports, and an autonomous AI Agent that executes multi-step workflows across the system.

> Built for the **Sababisha Solutions GTP 2026 Bootcamp** on the bootcamp-exact stack, plus an [own-stack] AI layer (Trigger.dev agents + MCP server).

## The seven systems (one monorepo)

| System | Folder | Stack |
|---|---|---|
| Backend / API | `backend/` | .NET 8 · ASP.NET Core Web API · EF Core 8 · Dapper · HotChocolate · SQL Server 2022 · Postgres 16 · Redis |
| Web | `web/` | React 18 · Vite 5 · MUI v6 · Apollo · Axios · TanStack Query 5 · Zustand |
| Mobile | `mobile/` | Flutter 3.19+ · Dart 3 · Riverpod · graphql_flutter · dio |
| DevOps / Infra | `infra/` | Docker 26+ · Compose v2 · Vercel · Railway · GitHub Actions |
| Quality Engineering | `qa/` | xUnit · Jest+RTL · Flutter tests · Cypress · Newman · k6 · OWASP |
| AI agents | `ai/` | Trigger.dev v3 (Copilot + scheduled agents) — [own-stack] — orchestrated by the .NET backend only; web/mobile never trigger or poll Trigger.dev, web may consume the scoped, read-only Copilot stream (`research/ai-integration.md` §2a) |
| MCP server | `mcp/` | @modelcontextprotocol/sdk — [own-stack] |

Each system is self-contained (own `AGENTS.md`, `.agents/skills/`, `project-kit/`). The root `AGENTS.md` + `docs/ARCHITECTURE.md` link them into one system.

## Status

**Backend/API implemented through spec 07** (EF Core 8 data layer, stored procedures + Dapper, REST controllers wired in `src/Griot.Api`, HotChocolate GraphQL layer, bulk operations, Auth: JWT + Argon2 + Redis). **Next: spec 08 (Postman API testing collection)** — auth endpoints are now live so the collection can cover the full flow (dependency order per `docs/DEPENDENCY-AUDIT.md`). Web (React) and Mobile (Flutter) are spec'd but **not yet scaffolded** — runbook commands for them below are the target conventions. See `docs/planning/`, `PROMPTS/`, and `project-kit/`.

- Docs: `docs/ARCHITECTURE.md`, `docs/database/DATABASE-DESIGN.md`, `docs/planning/` (NFR, capacity, risk, runbook, change-management)
- Diagrams: 12 design diagrams specified in `PROMPTS/week-02/` → to be generated in **Figma Make**
- ERD: 16 tables / 5 enums (decided pre-implementation — no schema rework later)

## Getting Started

### First-time setup (after git clone)

Binary files (PDFs, PowerPoints, screenshots) are stored in Cloudinary to avoid bloating the Git repository. Download them with:

```bash
./scripts/cloudinary-download.sh
```

This restores all research documents and screenshots (~15 MB, 14 files). See `docs/tooling/CLOUDINARY-BINARY-FILES.md` for details.

**What you get:**
- `research/GTP 2026 BOOTCAMP EDITION.pdf` — Official bootcamp spec
- `research/Netdata_RD_Presentation.pptx` — Monitoring strategy
- `research/screenshots/*.png` — Deployment evidence & UI references

## How to run & build (command reference)

> **Which folder:** every block says the exact directory to `cd` into first. Repo root = `~/sababisha/projects/gtp/griot`. Node auto-switches to v20 (`.nvmrc`) on `cd`. **Docker the container** is the real Docker Engine, not Podman (see `research/gtp-2026-prep.md` §6).

### 0. Prerequisites

| Tool | Version | Check |
|---|---|---|
| .NET SDK | 8.0.x (`global.json` pins `8.0.1xx`) | `dotnet --version` |
| Node.js | 20 (`.nvmrc`) | `node --version` |
| Flutter | 3.19+ (mobile only) | `flutter --version` |
| Docker Engine + Compose v2 | 26+ | `docker --version && docker compose version` |

### 1. Docker containers (SQL Server + Postgres + Redis) — from anywhere

The compose file lives **outside this repo** (shared Sababisha org stack): `~/sababisha/infra/docker-compose.yml`.

```bash
# Start the full local data stack (no-op if already running)
docker compose -f ~/sababisha/infra/docker-compose.yml up -d

# Check status / logs / stop
docker compose -f ~/sababisha/infra/docker-compose.yml ps
docker compose -f ~/sababisha/infra/docker-compose.yml logs -f sababisha-sqlserver
docker compose -f ~/sababisha/infra/docker-compose.yml down          # stop (keeps data volumes)
docker compose -f ~/sababisha/infra/docker-compose.yml down -v      # ⚠️ stop AND delete data volumes
```

| Service | Container name (compose prefixes `infra-`) | Host port |
|---|---|---|
| SQL Server 2022 | `infra-sababisha-sqlserver-1` | `localhost:14333` |
| PostgreSQL 16 | `infra-sababisha-postgres-1` | `localhost:5433` |
| Redis 7 | `infra-sababisha-redis-1` | `localhost:6380` |

Run SQL inside the container (password: `SababishaDev2026!` or `$SABABISHA_SA_PASSWORD`):

```bash
docker exec -i infra-sababisha-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'SababishaDev2026!' -C -d Griot \
  -Q "SELECT name FROM sys.tables ORDER BY name"
```

### 2. Backend API (.NET 8) — from `backend/`

```bash
cd backend
dotnet watch run --project src/Griot.Api   # dev server with hot reload
dotnet run --project src/Griot.Api        # no hot reload
dotnet build Griot.sln                    # compile everything
dotnet test                               # xUnit suite (when tests exist)
```

- App starts on **http://localhost:5064** (https profile: `https://localhost:7198`).
- **Swagger/OpenAPI:** http://localhost:5064/swagger · **Health:** http://localhost:5064/health
- Local dev secrets (DB connection string, JWT dev key) live in the **git-ignored** `backend/src/Griot.Api/appsettings.Local.json` — it is loaded automatically; for other environments set env vars instead:
  ```bash
  export ConnectionStrings__Default='Server=localhost,14333;Database=Griot;User Id=sa;Password=SababishaDev2026!;TrustServerCertificate=True'
  export JWT__Key='<dev key>' JWT__Issuer='Griot' JWT__Audience='GriotClients'
  ```

### 3. Transactional email — Brevo (OTP / admin notices)

Griot uses **Brevo** (brevo.com) for all transactional email (OTP codes, admin new-user notices).
Local secrets live in the git-ignored `backend/src/Griot.Api/appsettings.Local.json`:

```jsonc
"Brevo": {
  "ApiKey": "xkeysib-...",            // Settings → SMTP & API → API Keys
  "FromEmail": "you@example.com",      // REQUIRED — must be verified in Brevo
  "FromName": "Griot",
  "ContactToEmail": "info.donartkins.ke@gmail.com"  // admin new-user notice inbox
}
```

- **`Brevo:ApiKey` / `BREVO_API_KEY`** — Brevo SMTP/API key (`xkeysib-...`).
- **`Brevo:FromEmail` / `BREVO_FROM_EMAIL`** — **REQUIRED.** Add + verify a sender in Brevo
  (Settings → Senders) or verify a domain (Settings → Senders/Domains) and use an address on it.
- **`Brevo:FromName` / `BREVO_FROM_NAME`** — sender display name (default `Griot`).
- **`Brevo:ContactToEmail` / `CONTACT_TO_EMAIL`** — inbox for the "New user registered" admin notice
  (default `info.donartkins.ke@gmail.com`).

Docker/env equivalents use `__` (e.g. `Brevo__ApiKey`, `BREVO_API_KEY`).

> **Why Brevo?** The previous provider (Resend) restricted test-mode delivery to the account owner's
> email / a verified domain, which blocked sending OTP to arbitrary test recipients on the free tier.
> Brevo sends to any recipient on its free plan (300 emails/day) with a verified sender — no recipient
> sandbox. Free limit: **300 emails/day** (resets daily). No credit card required.
> Rate limits: keep under 300/day total; each `/api/auth/otp/request` = 1 transactional email.

Full detail + troubleshooting: `docs/security/AUTHENTICATION-GUIDE.md` §4.10/§7/§8 and `docs/api/auth-contract.md`.
The communication architecture — Email-only (spec 12 final state): multi-sender identities
(security/admin/noreply/support/info/team), rate-limit budget and Vercel/Railway env vars — lives in
**`docs/communication/COMMUNICATION-GUIDE.md`**.

### 4. Database — EF Core migrations & stored procedures — from `backend/`

`dotnet-ef` 8.0.30 is pinned in `backend/.config/dotnet-tools.json` (restore it with `dotnet tool restore` once if missing). The connection comes from `appsettings.Local.json` or `ConnectionStrings__Default`.

```bash
cd backend

# Create a new migration after changing an entity (DbContext lives in Griot.Infrastructure)
dotnet ef migrations add <Name> --project src/Griot.Infrastructure --startup-project src/Griot.Api

# Apply pending migrations to the local DB (Griot@localhost:14333)
dotnet ef database update --project src/Griot.Infrastructure --startup-project src/Griot.Api

# See what's already applied / roll back to a specific migration
dotnet ef migrations list --project src/Griot.Infrastructure --startup-project src/Griot.Api
dotnet ef database update 0 --project src/Griot.Infrastructure --startup-project src/Griot.Api   # ⚠️ reverts ALL
```

Stored procedures are versioned as files under `backend/src/Griot.Infrastructure/Sql/` and applied directly to SQL Server (idempotent — safe to re-run):

```bash
cd backend
docker exec -i infra-sababisha-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'SababishaDev2026!' -C -d Griot \
  < src/Griot.Infrastructure/Sql/usp_BulkUpdateTaskStatus.sql
docker exec -i infra-sababisha-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'SababishaDev2026!' -C -d Griot \
  < src/Griot.Infrastructure/Sql/usp_GetDashboardSummary.sql
```

**DBeaver:** new connection → SQL Server → host `localhost`, port `14333`, database `Griot`, user `sa`, password `SababishaDev2026!` (or `$SABABISHA_SA_PASSWORD`), enable **Trust server certificate**. Right-click database → Refresh to see new tables/procs. Stored procedures appear under **Stored Procedures** — not as tables.

### 5. Web app (React + Vite + MUI) — from `web/`

> 🚧 **Not scaffolded yet** — `web/` currently holds the design kit (`src/theme.ts`) and specs. Once the app lands (web feature specs 01–05), the commands are:

```bash
cd web
npm install                 # first time (or after pulling new deps)
npm run dev                 # Vite dev server (default http://localhost:5173)
npm run build               # production build → dist/
npm run lint && npm run typecheck && npm test   # verification gates
```

### 6. Mobile app (Flutter) — from `mobile/`

> 🚧 **Not scaffolded yet** — `mobile/` currently holds `lib/core/theme` and specs. Once built out (mobile/feature-specs 01–07), the commands are:

```bash
cd mobile
flutter pub get                      # fetch Dart packages (first time / after pubspec change)
flutter run                          # run on connected device/emulator (hot reload: r)
flutter analyze && flutter test      # verification gates
flutter build apk --release          # ⚠️ build installable APK
# Output: build/app/outputs/flutter-apk/app-release.apk
flutter build apk --debug            # faster debug APK for device testing
```

### 7. AI agents & MCP (Trigger.dev / MCP) — from `ai/` and `mcp/`

```bash
cd ai && npm install && npm run dev      # start local Trigger.dev dev server (agents/Copilot)
cd ai && npm run deploy                  # trigger deploy (production)

cd mcp && npm install && node index.js   # start the MCP server (contract tests via npm test)
```

> **Orchestration contract** (`research/ai-integration.md` §2a): Trigger.dev is a standalone compute/orchestration adapter — the **.NET backend triggers tasks** (server-to-server `TRIGGER_SECRET_KEY`) and **writes results back through the API** (`POST /api/webhooks/trigger` HMAC / `GRIOT_SERVICE_TOKEN` REST). Trigger.dev never owns domain data. Web and mobile never call Trigger.dev's public API — they call the .NET API; the only direct web↔Trigger channel is the read-only Copilot realtime stream (scoped access token).

### 8. End-to-end smoke check (backend)

```bash
cd backend
dotnet watch run --project src/Griot.Api                 # terminal 1 — wait for "Now listening on http://localhost:5064"
curl -s http://localhost:5064/health                      # → Healthy
curl -s http://localhost:5064/api/workspaces              # → {"message":"Not implemented yet"} (scaffold stub)
open http://localhost:5064/swagger                        # browse every route + Try it out
```

## Quick links

- **Docs**: `docs/README.md` · **Architecture**: `docs/ARCHITECTURE.md` · **Database**: `docs/database/DATABASE-DESIGN.md`
- **Diagrams**: `diagrams/README.md` · **Prompts**: `PROMPTS/README.md`
- **Contribute**: `CONTRIBUTING.md` · **Security**: `SECURITY.md` · **License**: `LICENSE` (MIT)

## Roadmap (bootcamp)

Week 1 Fundamentals · Week 2 Backend & API · Week 3 Frontend · Week 4 Mobile · Week 5 Deploy & DevOps · Week 6 QE foundations · Week 7 Real-world QE. Details per week in `research/`.

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
_Griot — the record of what the team built, and how well they built it._