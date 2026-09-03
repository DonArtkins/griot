# 🚀 GTP 2026 — Master Strategy Doc: Project "Griot"
> **Sababisha Solutions Graduate Training Programme — Bootcamp Stack adoption, architecture, environment setup**
> **Source of truth for the stack: `GTP 2026 BOOTCAMP EDITION.pdf` "2026 Core Technology Stack"**
> **OS: Parrot OS (Debian-based) · Editor: VSCodium · Containers: Docker Engine (real, not the Podman shim)**

---

## 0. What changed and why this doc was rewritten (again)

The previous version of this research swapped the bootcamp's stack for a personal Node/Express/Prisma/Next.js stack.
That is reversed here: **the bootcamp roadmap defines the company/cohort stack, everyone on the programme uses it, and "Griot" now runs exactly on it.** No capstone-specific substitutions for backend/frontend/mobile/devops.

The only intentional differences from the PDF are two kinds, both explicit and small:

1. **Machine-level (Parrot OS) adjustments** — because the host is Parrot (Debian-based), not Windows:
   - **SQL Server 2022** runs on Linux via the official container image (there is no native Debian package); primary database stays SQL Server, same version (2022) the guide names.
   - **Visual Studio 2022 → VS Code + C# Dev Kit** (Linux has no Visual Studio; the experience is identical for Web API work).
   - **Docker Desktop → Docker Engine + Docker Compose v2** installed the real way on Debian (not the Podman shim Parrot aliases by default — see gotcha).
   - Everything else (.NET 8, EF Core 8, Dapper, HotChocolate, React/Vite/MUI, Flutter, Vercel, GitHub Actions) installs/works on Linux exactly as documented by its vendor.

2. **Own-stack fills where the bootcamp defines nothing.** The guide defines no auth mechanism, no client-state library, no router, no motion library, no rate-limiting store, and no load-testing tool. In those gaps we use the already-proven personal stack (custom JWT auth, Zustand, React Router, GSAP/Lenis, Redis, k6) — each instance is explicitly marked **`[own-stack]`** in this document so the boundary between "bootcamp-mandated" and "personal choice" is always visible.

> Net effect: **the research matches the bootcamp stack line-by-line** (so Weeks 2–6 deliverables quote the guide verbatim), the environment stays Parrot-friendly, and the places the guide is silent are filled with tools already in the toolbox — never with tools that must be learned from scratch.
>
> **2026 add-on:** an **AI agent + MCP layer** (Trigger.dev v3 agents/workflows, a Griot MCP server, an in-app Copilot) is folded in as an own-stack extension — see `ai-integration.md`. It *wraps* the bootcamp backend (AI reads/writes only via the .NET/GraphQL API with a scoped service token) and adds zero substitutions to the core stack.

---

## 1. Project Name: Griot

**Griot** (single word, pronounced *GREE-oh*).

- **In-universe**: in Wakandan MCU/comics canon, Griot is Shuri's AI system — it runs her lab, tracks every invention, and holds the kingdom's accumulated technical knowledge. That's structurally what a project-management tool is: the system that rows a team's decisions, work, and history.
- **Real-world origin**: a *griot* is a West African oral historian / record-keeper — "the accurate, shared record of what happened and what's next" is exactly a PM tool's value proposition.

Alternates if it feels too on-the-nose once the UI is in front of you: **Djalia** (shared collective memory) and **Jabari** (mountain tribe known for discipline/focus). Griot remains the working name everywhere below.

---

## 2. The Tech Stack — Bootcamp Roadmap, Adopted (mapped 1:1 to the PDF)

The PDF's "2026 Core Technology Stack" is the contract. Every line is mapped to what Griot does with it. Every row has exactly one status: `exact` (we simply use it) or `[own-stack]` (the guide is silent — personal choice, flagged).

### 3.1 Backend Stack

| PDF item | Status | How Griot uses it |
|---|---|---|
| **.NET 8 (LTS)** | exact | Web API host; target framework `net8.0`. Installed via the official dotnet-install script on Parrot (no Visual Studio; CLI + C# Dev Kit). |
| **ASP.NET Core Web API (.NET 8)** | exact | The backend server: controllers (REST) + HotChocolate middleware (GraphQL) in one process. |
| **Entity Framework Core 8** | exact | Code-first migrations to SQL Server 2022 + LINQ queries. Replaces the old Prisma draft. |
| **Dapper 2.x** | exact | Hand-tuned SQL / stored-procedure hot paths and bulk operations (Week 2 "stored procedures & optimized queries"; pairs with EF Core in the same app). |
| **HotChocolate GraphQL 14+** | exact | GraphQL layer (queries, mutations, DataLoader-style batch loading). Week 3's Apollo Client talks to it. |
| **SQL Server 2022** | exact (containerized on Linux) | Primary database. Official `mcr.microsoft.com/mssql/server:2022-latest` image on Parrot (no native Debian pkg). Week 2 deliverables write against this DB. |
| **PostgreSQL 16** | exact | Provisioned as the guide lists it: **secondary/test** store + cohort exercises, not the capstone's primary DB. Both DBs in the same compose file. |

### 3.2 Frontend Stack

| PDF item | Status | Griot uses |
|---|---|---|
| **React 18.3+** | exact | `react@18.3` via Vite template. |
| **Vite 5+** | exact | Build/dev server; Vercel's "Vite framework preset" (Week 5 per the guide's steps). |
| **Material UI v6** | exact | Re-themed to Week 1 design tokens via `createTheme`. |
| **Apollo Client** | exact | GraphQL reads (dashboard/boards). |
| **Axios** | exact | REST calls, uploads, auth. |
| **TanStack Query v5** | exact | Server-state cache for REST data. |
| Client state (not in PDF) | **[own-stack] Zustand** | Filters/modals/drag state only — server data stays in Apollo/React Query caches. |
| Router (not in PDF) | **[own-stack] React Router v6/7** | Route splitting for Public/App shells + guards. |
| Motion (not in PDF) | **[own-stack] GSAP + ScrollTrigger + Lenis** | Public shell only (Awwwards face); App shell stays fast. |

### 3.3 Mobile Stack

| PDF item | Tool | Notes |
|---|---|---|
| **Flutter 3.19+ / Dart 3** | exact | Week 4; Android APK on Parrot (iOS needs Xcode/macOS → out of scope, see week-04). |
| **GraphQL Flutter** | exact | `graphql_flutter` → same HotChocolate endpoint. |
| **Provider / Riverpod** | **Riverpod** | The guide allows either; Riverpod is the closer match to Zustand's explicit-store model. |

### 3.4 DevOps & Deployment

| Item | Griot uses | Notes |
|---|---|---|
| **Docker 26+** | exact | Real Docker Engine (Parrot aliases docker→podman — fix per). |
| **Docker Compose v2** | exact | Local stack: api + sqlserver + postgres + redis. |
| **Vercel** | exact | Frontend hosting, Vite preset, env-var config (Week 5). |
| **GitHub Actions** | exact | PR-gated tests + deploy on main (Weeks 5–6). |
| **Azure / Railway / Render** | **Railway** (Render fallback; Azure variant documented in week-05) | All three are offered by the PDF; Railway chosen as Docker-native; Azure path written down for optional use. |

### Week-6 testing lines (same policy)
xUnit/NUnit → **xUnit** for .NET · Jest + RTL → exact · Flutter widget/integration → exact · Selenium/Cypress → **Cypress** · Postman/Newman → exact · perf testing (unnamed) → **[own-stack] k6** · OWASP + 80% coverage → exact.

> **Result:** the research content is the bootcamp stack with zero creative substitution on the backend/frontend/mobile platforms. The few `[own-stack]` rows are fields the guide never defined, and each one is invisible to graders (MUI still renders, REST still works, tests still gate CI).

### AI & Agent layer — own-stack extension (not in the PDF; added 2026)

| Piece | Tool | Notes |
|---|---|---|
| Background workflows & agents | **Trigger.dev v3** (TypeScript, Node 20) | scheduled tasks (digests, reminders, stale-board) + on-demand agents (Griot Copilot). Lives in the GTP tree as `ai/` with its own lockfile. |
| Agent tool-calling | Trigger agent tools → .NET GraphQL/REST | AI never writes to SQL Server directly; only via the API with a scoped service token. |
| Griot's MCP server (exposed) | `@modelcontextprotocol/sdk` (stdio + Streamable HTTP) | external AI clients (Claude Desktop, Cursor, VS Code Copilot, Cline) use Griot data as "Griot skills". |
| MCP client (consumed) | Trigger.dev MCP connections | Griot's agents call external MCP servers (Slack, GitHub, Notion, Linear) as skills. |
| In-app Copilot UI | MUI chat panel streaming via Trigger realtime | lives in `web/features/copilot`; mutation proposals require human approval. |
| LLM providers | OpenAI / Anthropic via Trigger.dev model config | keys exist only in the `ai/` project env, never in `web/`. |
| Cost & safety | Redis token budgets, restricted `ai-agent` role, audit log | in scope for the Week-6 OWASP pass. |

---

## 3. Architecture Philosophy: Two Surfaces, Not One (unchanged)

A "Project Management Tool" and "an Awwwards-winning site" pull in different directions — PM tools should be boring and fast; Awwwards sites spectacular. Real award-winning SaaS (Linear, Raycast, Arc) resolves this by **not treating the product as one surface**:

| | **Public Shell** | **App Shell** |
|---|---|---|
| Routes | `/`, `/pricing`, `/login`, `/signup` + marketing pages | everything behind auth: `/app/*` |
| Audience | Awwwards judges, prospects, first-time visitors | logged-in daily users |
| Design goal | Spectacle, story, proof-of-craft | Speed, clarity, information density |
| Motion | Full GSAP/ScrollTrigger/Lenis: cinematic hero, staggered reveals | Restrained — state-change transitions only |
| "Winning" looks like | Site of the Day submission-ready | A team could run a sprint in it without friction |

This split also decides where Vite's strengths land: code-splitting and lazy-loaded routes serve the Public Shell's first-paint; the App shell leans on client-side interactivity. Each week file carries the split forward.

> 2026 SaaS design research (already summarized in the old draft) supports this: best-converting SaaS sites show real product UI in the hero, place trust signals at decision points, and treat sub-2-second load as a conversion lever. The Public Shell budget is measured in Week 6/7 Lighthouse, not vibes.

---

## 4. Where to start: Figma Make, this week

Figma Make (the AI prototyping surface in the research screenshot) takes a structured prompt (optionally with reference images) and a "Plan" step before generating, with point-and-click refinement after. Full walkthrough — two ready-to-paste prompts (App shell first, Public shell second) + how to carry output into a proper Figma design system and then into the **EF Core schema** — lives in `week-01-fundamentals-and-system-design.md`. Two things to keep in mind:

- Treat the first pass as a **rough clay model**, not a final UI; budget 2–3 iteration rounds.
- Use **Plan mode deliberately for the App shell** (multi-screen, interconnected, shared state); the public marketing pages are fine as a single-shot generation.

---

## 5. The Seven Weeks — Index

| File | Covers (stack-exact now) |
|---|---|
| `week-01-fundamentals-and-system-design.md` | HackerRank (SQL + C#), Parrot env for the guide's stack, Figma Make design, **EF Core schema sketch** |
| `week-02-backend-api-development.md` | **ASP.NET Core 8** REST + **HotChocolate** GraphQL, **EF Core 8** + **Dapper**, **SQL Server 2022** (+ Postgres secondary), `[own-stack]` JWT auth, Redis, Postman |
| `week-03-frontend-development.md` | **React 18/Vite 5/MUI v6**, React Query 5 + Axios, Apollo Client, host-state, nested route shells |
| `week-04-mobile-development.md` | **Flutter 3.19+/Dart 3**, Riverpod, `graphql_flutter`, Android-only constraint on Parrot |
| `week-05-deployment-devops.md` | Docker multi-stage, Compose (api + SQL Server), Vercel (Vite preset), Railway/ Render/Azure, GitHub Actions |
| `week-06-quality-engineering-foundations.md` | **xUnit**/NUnit, Jest + RTL, Cypress, Newman, k6, OWASP on the owned auth, 80% gate |
| `week-07-real-world-qe-practice.md` | Full QE cycle on the deployed system, Jira, UAT, exec summary, Awwwards readiness |
| `ai-integration.md` | **Trigger.dev v3** agents/workflows, **Griot MCP server** + consumed MCP skills, Copilot UI, security & testing of the AI layer |

---

## 6. Environment Setup — Parrot OS, Everything Needed, Zero Collision with the Personal Stack

> Goal: one reproducible instructions file for a fresh Parrot machine with **both** stacks live:
> the GTP/bootcamp stack (this project) and the personal stack (unrelated work). Nothing below is a singleton global install that could break personal projects.

### 6.1 The Parrot-specific gotchas that still apply (unchanged)
1. **Parrot aliases `docker` to Podman by default.** `docker --version` on stock Parrot invokes Podman in compatibility mode. Install real Docker Engine (steps in 6.3) and confirm with `docker info | grep -i containerd` / the absence of the `podman` compatibility banner; optionally `sudo apt remove podman docker.io` if the shim shadows the real binary in `PATH`.
2. Run `sudo apt update && sudo apt full-upgrade -y` first — Parrot ships stripped repos.
3. **VS Code → VSCodium** stays (same source, telemetry stripped, Open VSX marketplace). The C# Dev Kit / C# extension is available on Open VSX, so there is no extension gap for the .NET stack.

### 6.2 Phase 0 — System prep (one-time, non-destructive)
```bash
sudo apt update && sudo apt full-upgrade -y
sudo apt install -y curl wget git build-essential unzip zip ca-certificates gnupg \
  apt-transport-https software-properties-common libssl-dev xz-utils
git config --global user.name "Don Artkins"
git config --global user.email "info.donartkins.ke@gmail.com"
```

### 6.3 Phase 1 — Real Docker Engine + Compose v2 (the only root-install in this guide)
```bash
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/debian/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian \
  $(. /etc/os-release && echo \"$VERSION_CODENAME\") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt update && sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo groupadd docker 2>/dev/null; sudo usermod -aG docker $USER   # re-login once
docker --version   # must say "Docker version 2x.x" – NOT podman
docker compose version
```

### 6.4 Phase 2 — The GTP backend stack containers (isolated by design)
One compose project `gtp` owns all of the programme's daemons, prefixed `gtp-*`, with distinct host ports so they **cannot collide** with personal Postgres/Redis:

```bash
mkdir -p ~/gtp && cd ~/gtp   # dedicated GTP home; keep personal work elsewhere
cat > docker-compose.yml <<'YAML'
services:
  gtp-sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${GTP_SA_PASSWORD:-GriotDev2026!}"
    ports: ["14333:1433"]            # note: host 14333 → container 1433 (no clash with anything else)
    volumes: [gtp_mssql:/var/opt/mssql]
  gtp-postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_PASSWORD: "${GTP_PG_PASSWORD:-griot-pg}"
    ports: ["5433:5432"]             # 5433 host → keeps personal 5432 untouched
    volumes: [gtp_pg:/var/lib/postgresql/data]
  gtp-redis:
    image: redis:7-alpine
    ports: ["6380:6379"]
volumes:
  gtp_mssql:
  gtp_pg:
YAML
docker compose up -d
docker exec gtp-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "GriotDev2026!" -C -Q "SELECT @@VERSION"
docker exec gtp-redis redis-cli ping        # PONG
```

> The `.env` with `GTP_SA_PASSWORD` is git-ignored in this repo; defaults here are development-only.

### 6.5 Phase 3 — .NET 8 SDK (per-user, version-pinned in the repo)
```bash
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 8.0
# add to ~/.bashrc: export PATH="$PATH:$HOME/.dotnet"
dotnet --version
dotnet new globaljson --sdk-version 8.0.1xx --roll-forward latestFeature   # inside the repo!
```
> `global.json` pins the SDK *per repo* → personal .NET usage elsewhere (if ever) is unaffected.

Global tools scoped to this repo only:
```bash
cd <griot-repo>/backend
dotnet new tool-manifest
dotnet tool install dotnet-ef   # EF Core migrations CLI for the bootcamp stack
```
```bash
# VS Code/C# support on this repo only (workspace settings, not global)
codium --install-extension ms-dotnettools.csharp
```

### 6.6 Phase 4 — Node 20 LTS for the Vite/mobile tooling (nvm, no global change)
```bash
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.1/install.sh | bash
nvm install 20 && nvm use 20 && echo "20" > .nvmrc   # .nvmrc keeps GTP repos on 20
node -v && npm -v
```
> Personal `nvm` default can stay at 24/whatever — GTP repos switch to 20 via `.nvmrc`.

### 6.7 Phase 5 — Frontend/mobile/test tooling
```bash
npm install -g pnpm               # monorepo-friendly
npm install -D @playwright/test   # (Cypress in weeks 5-6 uses its own deps)
flutter                          # see week-04 + Android toolchain
```

### 6.8 Phase 6 — AI layer tooling (own-stack, per-project, isolated)

```bash
cd ~/gtp/griot/ai && nvm use                 # .nvmrc → 20 (pinned)
npx trigger.dev@latest login                 # connect to the Trigger.dev cloud project
npx trigger.dev@latest init --project-ref <PROJECT_REF>
npm install @trigger.dev/sdk @trigger.dev/react-hooks
echo "ANTHROPIC_API_KEY=…" >> .env           # LLM keys live ONLY here, never in web/

cd ~/gtp/griot/mcp && npm init -y
npm i @modelcontextprotocol/sdk zod
npx @modelcontextprotocol/inspector node src/server.ts   # GUI smoke-test of the MCP tools
```

Both Node projects are separate from `web/` (own `.nvmrc`, own lockfiles) — same isolation rules as everything else; LLM keys never leave `ai/.env`.

## 7. Verification checklist (tick once after setup)

```bash
git --version && node -v && npm -v
dotnet --version && dotnet ef --version
docker --version && docker compose version          # real Engine, not podman
docker compose -f ~/gtp/docker-compose.yml ps      # gtp-sqlserver, gtp-postgres, gtp-redis up
docker exec gtp-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$GTP_SA_PASSWORD" -C -Q "SELECT @@VERSION"
docker exec gtp-redis redis-cli ping
psql -h localhost -p 5433 -U postgres -c "SELECT version();"
codium --version
flutter doctor
nvm ls                                           # 20 present; personal default untouched
npx trigger.dev@latest --version                # AI layer CLI (per-project)
npx @modelcontextprotocol/inspector --version   # MCP Inspector (per-project)
```

---

## 8. The Isolation Contract — GTP stack vs. the personal stack

The single most important operational rule for this whole programme:

| Question | Rule |
|---|---|
| Where do GTP projects live? | `~/gtp/…` (or this repo tree). Personal work lives *outside* it. |
| Global OS installs | Only Docker Engine + standard tooling .3). Everything else is per-user/per-repo. |
| Node versions | `nvm` per project: GTP repos carry `.nvmrc` → `20`; personal projects keep their own `.nvmrc`/engine. `nvm use` is per-shell, never a global default change. |
| .NET | Per-user SDK in `~/.dotnet` + **per-repo `global.json`** pinning 8.0; `.NET tools` scoped via `dotnet new tool-manifest`. |
| Databases | GTP daemons are Docker containers named `gtp-*` on non-default host ports (14333, 5433, 6380) — they cannot shadow a personal Postgres/Redis/MySQL on 5432/6379/3306. Data lives in `gtp_*` Docker volumes. |
| Package versions | GTP repos pin versions in lockfiles (`packages-lock.json`, `.NET` `Directory.Packages.props`). Personal repos are untouched by GTP installs. |
| CI/cloud | Vercel/Railway projects are separate; secrets are per-project. No shared credentials between "Griot" and personal apps. |
| AI layer | Trigger.dev project + MCP server live **inside the GTP repo** (`ai/`, `mcp/`) with their own lockfiles; LLM keys are per-GTP-project env, never in `web/` and never global. |
| "Contamination" boundary | The only shared resource is the Docker daemon itself — and even there, namespaces, networks and port mappings keep everything separate. |

**How to work daily:** open a terminal → `cd ~/gtp/griot` → `nvm use` (reads `.nvmrc` → 20) → `dotnet run` or `npm run dev` → containers already up via `docker compose`. Close it, `cd ~/work/mypersonalproject` → `nvm use` → personal versions. Nothing about the GTP toolchain is visible to the personal one and vice versa.

---
**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
**Griot — the record of what the team built, and how well they built it.**