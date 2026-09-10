# 🚀 GTP 2026 — Master Strategy Doc: Project "Griot"
> **Sababisha Solutions Graduate Training Programme — Bootcamp Stack adoption, architecture, environment setup**
> **Source of truth for the stack: `GTP 2026 BOOTCAMP EDITION.pdf` "2026 Core Technology Stack"**
> **OS: Parrot OS (Debian-based) · Editor: VSCodium · Containers: Docker Engine (real, not the Podman shim)**

---

## 0. What changed and why this doc was rewritten (again)

The previous version of this research swapped the bootcamp's stack for a personal Node/Express/Prisma/Next.js stack.
That is reversed here: **the bootcamp roadmap defines the company/cohort stack, everyone on the programme uses it, and "Griot" now runs exactly on it.** No capstone-specific substitutions for backend/frontend/mobile/devops.

**This revision also removes the Windows 10 VM entirely from this document.** It never touched the GTP toolchain and was cluttering the setup story — it now lives in its own file, `windows-vm-setup.md`, kept for later reference but no longer part of the Griot onboarding path.

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
| **Docker 26+** | exact | Real Docker Engine (Parrot aliases docker→podman — fix per §6.3). |
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
| Cost & safety | Redis token budgets, restricted real-user OBO role `ai-on-behalf-of`, audit log · AI reports (PDF/CSV) · system auditor | in scope for the Week-6 OWASP pass. |

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

## 6. Environment Setup — Parrot OS, Sababisha-Wide, Zero Collision with the Personal Stack

> **Scope correction (superseding the original per-GTP-only version of this section):** this setup is no longer GTP-only. It is **Sababisha-organization-wide** — one shared Docker Engine, one shared SQL Server/Postgres/Redis backing-services stack, used by **every** Sababisha project, GTP-derived or not. Griot (this GTP capstone) is one *consumer* of this infrastructure, not its owner. A future non-GTP Sababisha project reuses the exact same running containers — see §6.4b for how.
>
> Directory convention: `~/sababisha/infra/` (the shared compose file + backing services) and `~/sababisha/projects/<area>/<project-name>/` (every project's own code — GTP projects nest under `~/sababisha/projects/gtp/<project-name>`, e.g. `~/sababisha/projects/gtp/griot`). Personal, non-Sababisha work stays fully outside `~/sababisha/` — nothing below is a singleton global install that could break it.
>
> **The optional Windows 10 VM (VirtualBox) fallback surface has moved out of this document** — see `windows-vm-setup.md`. It is not required for, and does not touch, anything below.

### 6.1 The Parrot-specific gotchas that still apply (updated with real incidents from this machine)

1. **Parrot aliases `docker` to Podman by default — and this is now confirmed, not theoretical.** The first real run on this machine showed `docker --version` reporting a normal Docker Engine version string, but every `docker` command actually failed against `unix:///run/user/1000/podman/podman.sock` — the CLI genuinely was talking to Podman's rootless socket, not the Docker daemon, even though `docker-ce`/`docker-compose-plugin` were installed. **`docker --version` succeeding is not proof the real daemon is being used** — always check the failing socket path in the error, or `docker info | grep -i containerd`, to confirm which backend is answering.
2. **`systemctl status docker` reported `Loaded... disabled` / `Active: inactive (dead)`.** Installing the packages does not start or enable the daemon. Fix is `sudo systemctl enable --now docker`, then re-check `docker --version` / `sudo docker run hello-world` — do this as a standing step in §6.3, not an afterthought.
3. Run `sudo apt update && sudo apt full-upgrade -y` first — Parrot ships stripped repos.
4. **VS Code → VSCodium** stays (same source, telemetry stripped, Open VSX marketplace). The C# Dev Kit / C# extension is available on Open VSX and was already installed and confirmed on this machine — no extension gap for the .NET stack.
5. **Docker's `apt` repo has no `echo` (Parrot's codename) release — you must hardcode a real Debian codename.** Using `$(. /etc/os-release && echo "$VERSION_CODENAME")` on Parrot resolves to `echo`, which 404s against `download.docker.com` (Docker only publishes for actual Debian/Ubuntu codenames). Parrot 7.4 tracks **Debian 13 (trixie)** upstream, so the repo line must hardcode `trixie` — see the corrected command in §6.3.
6. **A reboot does not fix a failed install, and does not start a disabled daemon.** If `docker` commands fail after a restart, check both (a) whether the packages actually installed and (b) whether the daemon is enabled — see gotcha 2. Don't assume a fresh boot fixes either.
7. **`dotnet-install.sh` completing successfully does not put `dotnet` on `PATH` for future shells.** On this machine the script reported "Installation finished successfully" and even printed the version, but `dotnet --version` in the *same* and later shells returned `command not found` — the script only patches the current process's PATH, not `~/.bashrc`. The `export PATH="$PATH:$HOME/.dotnet"` line in §6.5 is not a suggestion; add it to `~/.bashrc` and `source ~/.bashrc` (or open a new shell) before trusting any `dotnet` command, including inside `backend/` for `dotnet new tool-manifest` / `dotnet tool install dotnet-ef`.
8. **`flutter` is not yet installed on this machine.** Every reference to it below (`flutter`, `flutter doctor`) currently returns `command not found`. This is a real gap, not a false alarm — install it before Week 4 per the guide's Android toolchain steps; it is intentionally not detailed further in this doc since Week 4 owns it.
9. **MCP Inspector v1 (`@modelcontextprotocol/inspector@0.15.0`) has a broken CLI arg parser on Node 20** — `npx @modelcontextprotocol/inspector node src/server.ts` throws `ERR_PARSE_ARGS_INVALID_OPTION_VALUE` on `--env` before it even reaches your server. The GUI mode (bare `npx @modelcontextprotocol/inspector`) works fine and was confirmed running on `http://127.0.0.1:6274` with a session token. Prefer the bare GUI invocation, or upgrade to v2 (`npm i @modelcontextprotocol/inspector@latest`) if the CLI form is actually needed — v1 only gets security fixes.
10. **A stray semicolon-prefixed command (`;s`) is a bash syntax error, not a typo worth chasing** — `bash: syntax error near unexpected token ';'` just means the previous line's terminal wrapping merged into the next; retype the intended command.
11. **The old standalone `docker-compose` package conflicts with `docker-compose-plugin` and will fail the install with a dpkg overwrite error** — both ship the same file at `/usr/libexec/docker/cli-plugins/docker-compose`. Confirmed on this machine: `docker-compose-plugin`'s unpack step failed with `trying to overwrite '.../docker-compose', which is also in package docker-compose`. Fix: `sudo apt remove -y docker-compose` first, then `sudo apt install -y docker-compose-plugin`. The §6.3 block below now removes `docker-compose` up front for this reason.
12. **`docker` commands can still hit `podman.sock` even after the daemon is enabled and running, if `DOCKER_HOST` is set system-wide.** This showed up *after* `systemctl enable --now docker` had already succeeded (`Active: active (running)` confirmed) and `hello-world` had already run cleanly, which ruled out a daemon problem. Root cause, confirmed on this machine: `/etc/profile.d/podman-docker.sh` — a script installed by the `podman-docker` package — auto-exports `DOCKER_HOST=unix:///run/user/1000/podman/podman.sock` for every login shell whenever it isn't already set, and it isn't limited to any one dotfile, so `grep`ing `~/.bashrc`/`~/.zshrc`/`~/.profile` alone won't find it. `unset DOCKER_HOST` and `systemctl --user unset-environment DOCKER_HOST` both look like fixes but don't stick, since the script re-exports it on every new login shell. **Permanent fix:** `sudo mv /etc/profile.d/podman-docker.sh /etc/profile.d/podman-docker.sh.disabled`, then open a completely fresh terminal (not a new tab) and confirm `echo $DOCKER_HOST` prints nothing. If it's ever unclear where a stray `DOCKER_HOST` is coming from again, check `/etc/profile.d/*.sh` and `systemctl --user show-environment`, not just personal dotfiles.
13. **A first `docker compose up -d` pull looks stalled but usually isn't.** `mssql/server:2022-latest` is a large image (well over 1GB); watching "Pulling" with no progress bar movement for a while is normal on the first run, especially over a slower connection — it is not the same failure mode as gotchas 1–2 above. Give it time before assuming something is broken; `docker compose ps` in a second terminal shows real status if in doubt.
14. **Compose auto-prefixes container names with the project folder name — plain `docker exec sababisha-sqlserver ...` will fail with "No such container".** Running `docker compose up -d` from `~/sababisha/infra/` (folder name `infra`) produced containers named `infra-sababisha-sqlserver-1`, `infra-sababisha-postgres-1`, `infra-sababisha-redis-1` — not the bare `sababisha-*` names used throughout this doc's example commands. Confirm actual names with `docker compose ps` before running any `docker exec ...`, or add an explicit `container_name:` field per service in `docker-compose.yml` (e.g. `container_name: sababisha-sqlserver`) so the short names always match — the latter is the more permanent fix and avoids retyping the `infra-`-prefixed name everywhere.
15. **`dotnet tool install dotnet-ef` with no version pin grabs the newest release, not one matching the SDK.** On this machine that meant `dotnet-ef 10.0.11` installed alongside a `.NET 8.0.424` SDK / EF Core 8 packages — a major-version gap that risks migration failures later even though `dotnet ef --version` reports success. Since this project's stated contract is exact bootcamp-stack parity, always pin explicitly: `dotnet tool install dotnet-ef --version 8.0.*` (confirmed resolving to `8.0.30` on this machine). Re-run `dotnet tool uninstall dotnet-ef` first if it's already installed unpinned.
16. **A single `.nvmrc` at a repo's root does not cover its subdirectories.** `nvm use` (manual or auto-triggered) only checks the *current* directory for `.nvmrc` — `cd`ing straight into `backend/`, `web/`, `mcp/`, etc. (the normal daily flow) silently misses a root-level `.nvmrc` and falls back to whatever Node version was already active, with no warning. Confirmed on this machine: `node -v` inside `backend/` kept reporting the personal default (`v24.20.0`) instead of the project's pinned `20`. See the fix — a `.nvmrc` seeded into every subdirectory, plus an optional parent-walking `cd()` shell override — in §6.6.

### 6.2 Phase 0 — System prep (one-time, non-destructive)
```bash
sudo apt update && sudo apt full-upgrade -y
sudo apt install -y curl wget git build-essential unzip zip ca-certificates gnupg \
  apt-transport-https software-properties-common libssl-dev xz-utils
git config --global user.name "Don Artkins"
git config --global user.email "info.donartkins.ke@gmail.com"
```

### 6.3 Phase 1 — Real Docker Engine + Compose v2 (the only root-install in this guide, Sababisha-wide — one install serves every project)

**⚠️ Do not substitute `$VERSION_CODENAME` for the codename below.** It resolves to `echo` on Parrot, which does not exist in Docker's repo and 404s. Hardcode `trixie` (Parrot 7.4's Debian base) instead.

```bash
# Clean up first if a previous attempt left a broken repo file behind
sudo rm -f /etc/apt/sources.list.d/docker.list /etc/apt/keyrings/docker.gpg

# Remove conflicting unofficial packages (safe to run even if none are installed)
sudo apt remove -y podman docker.io docker-compose docker-compose-v2 docker-doc docker-buildx podman-docker containerd runc 2>/dev/null

sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/debian/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

# Hardcoded 'trixie', NOT $VERSION_CODENAME — see warning above
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian trixie stable" | sudo tee /etc/apt/sources.list.d/docker.list

sudo apt update

# Confirmed necessary on this machine: docker-compose-plugin will fail to unpack
# ("trying to overwrite '.../docker-compose', which is also in package docker-compose")
# if the old standalone docker-compose package is still present — remove it explicitly,
# don't rely on the earlier blanket removal line catching it.
sudo apt remove -y docker-compose 2>/dev/null

sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo usermod -aG docker $USER   # docker group is created automatically by the package; no need for groupadd

# Confirmed necessary on this machine — installing the package does not start or enable it:
sudo systemctl enable --now docker

# Confirmed useful on this machine — a stale env var can silently redirect docker
# commands to Podman's socket even with the real daemon running:
echo $DOCKER_HOST   # should print nothing; if it shows a podman.sock path, run: unset DOCKER_HOST
type docker         # should say "docker is /usr/bin/docker" — not an alias or function
```
**Log out and back in now** (or `newgrp docker` for the current shell only) — group membership does not apply retroactively, and every command below silently fails with "permission denied" or a Podman-socket "command not found"-looking error otherwise.

```bash
docker --version           # must say "Docker version 2x.x" — NOT podman
docker compose version
sudo systemctl status docker   # confirm Active: active (running), not inactive/disabled
sudo docker run hello-world    # confirms pull + run works end-to-end
```

**If `docker` commands still fail after all of this:** check which failure you actually have —
- `command not found` / `apt update` errored on a 404 or missing Release file → the install never completed; re-run the block from the top.
- A working `docker --version` but every real command errors against `unix:///run/user/1000/podman/podman.sock` → the daemon isn't the one answering; re-check `sudo systemctl status docker` and re-run `sudo systemctl enable --now docker`, then retry `sudo docker run hello-world`.
- `Active: inactive (dead)` in `systemctl status` → the daemon was never started; `sudo systemctl enable --now docker` fixes this specifically.

A reboot alone does not fix any of the three.

### 6.4 Phase 2 — The Sababisha-wide backing-services stack (supersedes the old per-GTP `gtp-*` version)

**Superseded:** the original version of this section stood up a `gtp`-only compose project at `~/gtp/docker-compose.yml` with `gtp-*`-prefixed containers. That has been torn down (`docker compose down` + `rm ~/gtp/docker-compose.yml`) and replaced with the version below, which lives at the **Sababisha org level** and is shared by every project.

One compose project — `sababisha-infra` — owns one running SQL Server, one Postgres, and one Redis for the **entire organization**. Every Sababisha project (GTP-derived like Griot, or standalone) connects to these same containers; isolation between projects happens at the *database* level (one database per project on the shared SQL Server instance), not by duplicating the whole engine per project.

```bash
mkdir -p ~/sababisha/infra ~/sababisha/projects
cd ~/sababisha/infra
cat > docker-compose.yml <<'YAML'
services:
  sababisha-sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${SABABISHA_SA_PASSWORD:-SababishaDev2026!}"
    ports: ["14333:1433"]
    volumes: [sababisha_mssql:/var/opt/mssql]
  sababisha-postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_PASSWORD: "${SABABISHA_PG_PASSWORD:-sababisha-pg}"
    ports: ["5433:5432"]
    volumes: [sababisha_pg:/var/lib/postgresql/data]
  sababisha-redis:
    image: redis:7-alpine
    ports: ["6380:6379"]
volumes:
  sababisha_mssql:
  sababisha_pg:
YAML

docker compose up -d
docker exec sababisha-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "SababishaDev2026!" -C -Q "SELECT @@VERSION"
docker exec sababisha-redis redis-cli ping        # PONG
```

> The `.env` with `SABABISHA_SA_PASSWORD` is git-ignored; defaults here are development-only. Host ports (14333, 5433, 6380) are chosen the same way the old `gtp-*` setup chose them — to avoid colliding with any personal-stack Postgres/Redis/SQL Server running on the default ports (5432, 6379, 1433).

Create Griot's own database on the shared instance (one `CREATE DATABASE` per project, not a new container per project):
```bash
docker exec -it sababisha-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "SababishaDev2026!" -C -Q "CREATE DATABASE Griot"
```
Griot's connection string points at `Server=localhost,14333;Database=Griot;...` — same shared server, isolated purely by database name.

**Confirm the daemon is actually up before running any of the above** — this whole phase depends on §6.3's daemon-enabled fix; if `sababisha-sqlserver` won't start or `docker compose up -d` hangs, go back and confirm `sudo systemctl status docker` shows `active (running)` first.

### 6.4a Project layout on disk (current, as of this incident's cleanup)
```
~/sababisha/
├── infra/
│   └── docker-compose.yml          # the ONE shared compose file — sababisha-sqlserver/-postgres/-redis
└── projects/
    └── gtp/
        └── griot/                  # moved here from its original location during setup
            ├── backend/            # per-project .NET SDK pin (§6.7) + EF tools live HERE, not at infra level
            └── ...
```
Personal, non-Sababisha work stays entirely outside `~/sababisha/` and is untouched by any of this.

### 6.4b How another Sababisha or GTP project plugs into this (no new containers, ever)

This is the actual point of moving the setup to org level: **a new project never re-runs §6.3 or §6.4.** Docker Engine and the `sababisha-infra` containers are installed and started exactly once per machine. Onboarding a new project — whether it's a future GTP cohort deliverable or an unrelated Sababisha product — is just:

1. **Create its folder** under the right area:
   ```bash
   mkdir -p ~/sababisha/projects/gtp/<new-project-name>        # another GTP project
   mkdir -p ~/sababisha/projects/<new-project-name>             # a non-GTP Sababisha project
   ```
2. **Make sure the shared infra is running** (it usually already is — this is a no-op if so):
   ```bash
   docker compose -f ~/sababisha/infra/docker-compose.yml up -d
   ```
3. **Create one database for it** on the already-running SQL Server (and/or a schema on the shared Postgres, if it needs Postgres instead):
   ```bash
   docker exec -it sababisha-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "SababishaDev2026!" -C -Q "CREATE DATABASE <NewProjectName>"
   ```
4. **Point its own connection string** at `localhost,14333` / `localhost,5433` / `localhost,6380` with its own database/schema name — never a new container, new port, or new compose file.
5. **Pin its own `.NET` SDK/EF tools per-repo** (§6.7) if it's a .NET project — this part stays per-project by design, since different Sababisha projects may need different .NET/EF versions over time; only the database *engines* are shared, not the tooling versions.

The only thing that ever needs `sudo` again on this machine, for any future project, is a Docker Engine version upgrade (§6.3's "Upgrade" path) — never a fresh install.

### 6.4c DBeaver — the database GUI (per-user, one install, works for every project)

Matches the guide's Week 1 tooling line (`DBeaver 24+`). Installed once per machine, like Docker — not per-project.

```bash
cd ~/Downloads   # or wherever the .deb was downloaded
sudo apt install ./dbeaver-ce-*.deb
```
`apt install ./file.deb` (not bare `dpkg -i`) so dependency resolution happens automatically. If it still errors on missing deps:
```bash
sudo dpkg -i dbeaver-ce-*.deb
sudo apt --fix-broken install
```

**Launching it:** the app-menu search surfaces two near-identical entries — the correct one is **"DBeaver Community"**, not the bare `dbeaver-ce` result (that's just the underlying package/binary name matched by the search index, not a proper desktop launcher). From a terminal, `dbeaver &` works either way.

**Connecting to the shared SQL Server:** New Database Connection → **SQL Server** → host `localhost`, port `14333` (the remapped port from §6.4, not the default 1433), database `Griot`, SQL Server Authentication, user `sa`, password `SababishaDev2026!` (or `$SABABISHA_SA_PASSWORD` if overridden). Under the SSL/driver-properties tab, enable **Trust server certificate** — required since the container's cert is self-signed; the connection test fails without it. First connect prompts to download the SQL Server JDBC driver — accept it.

**Connecting to the shared Postgres:** same flow, driver **PostgreSQL**, host `localhost`, port `5433`, user `postgres`, password `sababisha-pg` (or `$SABABISHA_PG_PASSWORD`).

Both connections are saved once and reused for every Sababisha project on this machine — a new project (per §6.4b) just means expanding the existing SQL Server connection's tree to the newly created database, not creating a new DBeaver connection.

**Schema-authority rule:** for Griot, DBeaver's own table-creation UI is for ad-hoc inspection and scratch tables only — the authoritative schema is EF Core migrations (§6.5, `dotnet ef migrations add` / `database update`). Hand-editing an EF-owned table's structure directly in DBeaver will desync the migration history from the live schema; use it to *read* and verify, not to *redefine* what EF already owns. Full connect/browse/create-table/disconnect walkthrough, with screenshots-equivalent step-by-step, lives in `gtp-user-manual.md` §6.3.

### 6.5 Phase 3 — .NET 8 SDK (per-user, version-pinned in the repo)
```bash
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 8.0

# Confirmed necessary on this machine — the script itself does NOT persist PATH:
echo 'export PATH="$PATH:$HOME/.dotnet"' >> ~/.bashrc
source ~/.bashrc

dotnet --version
dotnet new globaljson --sdk-version 8.0.1xx --roll-forward latestFeature   # inside the repo!
```
> `global.json` pins the SDK *per repo* → personal .NET usage elsewhere (if ever) is unaffected.
>
> **If `dotnet --version` still says `command not found` after the install script reports success:** this is the PATH issue, not a failed install — confirm the `export` line actually landed in `~/.bashrc` (not just the current shell) and open a fresh terminal.

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
> Personal `nvm` default can stay at 24/whatever — GTP repos switch to 20 via `.nvmrc` seeded in every subdirectory (see the gotcha above), or automatically via the parent-walking `cd()` shell override.

**Confirmed gotcha on this machine — a single root `.nvmrc` is not enough.** `nvm use` (and any manual `.nvmrc` check) only looks in the *current* directory. `cd`ing straight into a subfolder like `backend/`, `web/`, or `mcp/` — which is the normal daily flow — silently misses a `.nvmrc` that only exists at the repo root and falls back to the personal default (24), with no error or warning. This bit real work on this machine: `node -v` inside `backend/` kept reporting `v24.20.0` even though the root `.nvmrc` said 20.

**Fix (two layers, both applied on this machine):**

1. **A `.nvmrc` in every directory that gets `cd`'d into directly** — not just the repo root. Seeded across all of `~/sababisha/projects/gtp/griot/` (backend/, web/, mobile/, ai/, mcp/, infra/, qa/, docs/, research/, scripts/, and further nested subfolders — 123 files total on this machine, content: `20`). This means even a bare `cd` + `nvm use` in any subfolder works correctly with zero shell customization, and it's what actually lands in the repo/git history for any teammate.

2. **A parent-walking `cd` override in `~/.bashrc`**, so Node switches automatically on every `cd` — no need to remember to run `nvm use` by hand:
   ```bash
   autoload_nvmrc() {
     local dir="$PWD"
     while [ "$dir" != "/" ]; do
       if [ -f "$dir/.nvmrc" ]; then
         nvm use --silent
         return
       fi
       dir="$(dirname "$dir")"
     done
     nvm use default --silent
   }
   cd() {
     builtin cd "$@" && autoload_nvmrc
   }
   autoload_nvmrc
   ```
   This walks upward from the current directory looking for the nearest `.nvmrc` (so it works correctly even for a stray subfolder that doesn't have its own `.nvmrc`), and falls back to `nvm use default` — requires `nvm alias default 24` (or whatever the personal default is) to be set once — when nothing is found anywhere up the tree. Purely a local shell convenience; it is **not** committed to the repo and has no effect on teammates or CI.

**Verified on this machine, fresh interactive shell, both layers active:**
```
cd ~/sababisha/projects/gtp/griot/backend → v20.20.2
cd ~/sababisha/projects/gtp/griot/web     → v20.20.2
cd ~/sababisha/projects/gtp/griot/mcp     → v20.20.2
cd ~                                      → v24.20.0 (personal default, untouched)
```

> If the project's required Node version ever changes, remember both layers need updating: every seeded `.nvmrc` file (bulk `find ... -name .nvmrc -exec` is faster than editing 123 files by hand) — the `cd()` function itself needs no change, since it just reads whatever `.nvmrc` says.

### 6.7 Phase 5 — Frontend/mobile/test tooling
```bash
npm install -g pnpm               # monorepo-friendly
npm install -D @playwright/test   # (Cypress in weeks 5-6 uses its own deps)
flutter                           # NOT YET INSTALLED on this machine — see gotcha 8 above; install before Week 4
```

### 6.8 Phase 6 — AI layer tooling (own-stack, per-project, isolated)

```bash
cd ~/sababisha/projects/gtp/griot/ai && nvm use     # .nvmrc → 20 (pinned)
npx trigger.dev@latest login                 # connect to the Trigger.dev cloud project
npx trigger.dev@latest init --project-ref <PROJECT_REF>
npm install @trigger.dev/sdk @trigger.dev/react-hooks
echo "ANTHROPIC_API_KEY=…" >> .env           # LLM keys live ONLY here, never in web/

cd ~/sababisha/projects/gtp/griot/mcp && npm init -y
npm i @modelcontextprotocol/sdk zod
npx @modelcontextprotocol/inspector             # GUI smoke-test — bare invocation; see gotcha 9 re: the v1 CLI --env bug
```

Both Node projects are separate from `web/` (own `.nvmrc`, own lockfiles) — same isolation rules as everything else; LLM keys never leave `ai/.env`. Trigger.dev login was confirmed working end-to-end on this machine (device-code flow, account retrieved).

## 7. Verification checklist (tick once after setup)

```bash
git --version && node -v && npm -v
dotnet --version && dotnet ef --version
docker --version && docker compose version           # real Engine, not podman
sudo systemctl status docker                          # must show active (running), not inactive/disabled
docker compose -f ~/sababisha/infra/docker-compose.yml ps   # sababisha-sqlserver, sababisha-postgres, sababisha-redis up
docker exec sababisha-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SABABISHA_SA_PASSWORD" -C -Q "SELECT @@VERSION"
docker exec sababisha-redis redis-cli ping
psql -h localhost -p 5433 -U postgres -c "SELECT version();"
codium --version
dbeaver --version 2>/dev/null || dpkg -l | grep dbeaver-ce   # confirm DBeaver is actually installed
flutter doctor                                   # expect command-not-found until Flutter is installed — see §6.7
nvm ls                                           # 20 present; personal default untouched
npx trigger.dev@latest --version                # AI layer CLI (per-project)
npx @modelcontextprotocol/inspector --version   # MCP Inspector (per-project) — bare GUI form, not --env flags
```

---

## 8. The Isolation Contract — Sababisha stack vs. the personal stack (and how projects share within Sababisha)

There are now **two** boundaries in play, not one: Sababisha-wide vs. personal (unchanged in spirit), and — new — Sababisha **infra** (shared) vs. Sababisha **project** (isolated per-project). The single most important operational rule for this whole setup:

| Question | Rule |
|---|---|
| Where does Sababisha work live? | `~/sababisha/` — `infra/` for the one shared compose file, `projects/<area>/<name>/` for every project's own code (GTP projects nest under `projects/gtp/`). Personal work lives *outside* `~/sababisha/` entirely. |
| Global OS installs | Only Docker Engine (§6.3) + standard CLI tooling (§6.2). Installed **once**, used by every current and future Sababisha project. Everything else is per-user/per-repo. |
| Databases — shared or per-project? | **Shared engine, isolated by database.** One `sababisha-sqlserver`, one `sababisha-postgres`, one `sababisha-redis` container (§6.4) on fixed non-default host ports (14333, 5433, 6380) — chosen so they never collide with a personal Postgres/Redis/SQL Server on the OS defaults (5432, 6379, 1433). Every project gets its **own database** on the shared SQL Server (`CREATE DATABASE Griot`, `CREATE DATABASE <NextProject>`, ...) rather than its own container. Data lives in `sababisha_mssql`/`sababisha_pg` Docker volumes — shared volumes, project-separated by database name inside them. |
| Node versions | `nvm` per project: each Sababisha repo carries a `.nvmrc` in every subdirectory (GTP repos → `20`, not just the root — see §6.6 gotcha), plus a local parent-walking `cd()` shell override so the correct version loads automatically; personal projects keep their own `.nvmrc`/engine. Never a global default change — confirmed the personal default (24) stays untouched outside Sababisha folders. |
| .NET | Per-user SDK in `~/.dotnet` (installed once, §6.5) + **per-repo `global.json`** pinning a version per project — this stays per-project on purpose, since different Sababisha projects may need different .NET/EF versions over time even though they share the same database engine. `.NET tools` scoped via `dotnet new tool-manifest` per repo. |
| Package versions | Every project pins its own lockfiles (`package-lock.json`, `.NET` `Directory.Packages.props`). No project's installs affect another's, or the personal stack's. |
| CI/cloud | Vercel/Railway/Azure projects are separate per Sababisha project; secrets are per-project. No shared credentials between Griot, any other Sababisha project, and personal apps. |
| AI layer | Trigger.dev project + MCP server live **inside each project's own repo** (`ai/`, `mcp/`) with their own lockfiles; LLM keys are per-project env, never shared across projects and never global. |
| "Contamination" boundary | The only resources genuinely shared across *all* Sababisha projects are the Docker daemon and the three `sababisha-*` containers themselves — and even there, one database per project inside them keeps data fully separated. Nothing is shared with the personal stack at any layer. |

**Onboarding a brand-new Sababisha project** (GTP or otherwise) is §6.4b, in full — in short: make its folder, confirm `sababisha-infra` is already up (it usually is), create its one database, point its connection string at it, done. No new containers, no new Docker install, no new `sudo`.

**How to work daily:** open a terminal → `docker compose -f ~/sababisha/infra/docker-compose.yml up -d` (no-op if already running) → `cd ~/sababisha/projects/gtp/griot` (or straight into any subfolder like `backend/`) → Node switches to 20 automatically on `cd` (parent-walking shell override + seeded `.nvmrc` in every subfolder — see §6.6) → `dotnet run` or `npm run dev`. Switching to a different Sababisha project is just `cd ~/sababisha/projects/<other>` — the same running `sababisha-*` containers serve it too, no restart needed. Closing out and moving to personal work: `cd ~/work/mypersonalproject` → Node reverts to the personal default automatically (no `.nvmrc` found anywhere up the tree). Nothing about the Sababisha toolchain is visible to the personal one and vice versa.

---
**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
**Griot — the record of what the team built, and how well they built it.**

## Implemented authentication contract (Feature 07)

Use the [auth contract](../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.
