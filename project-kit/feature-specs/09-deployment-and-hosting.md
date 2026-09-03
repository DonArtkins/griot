# Feature 09 — Deployment & Hosting (each app, its own story)

## Type

NEW FEATURE

## What This Delivers

Every Griot app deployed where it best fits, per `research/week-05-deployment-devops.md`: **web → Vercel** (Vite preset), **backend → Railway** (Docker, with Render + Azure variants documented), **mcp → Railway** (Streamable HTTP Docker), **ai → Trigger.dev cloud**, **mobile → APK artifact**. `docker-compose.yml` remains the local parity mirror of this entire topology.

## Dependencies

- Features 04–07 (each app has a deployable slice).
- Feature 08 (CI/CD deploys on `main`).

## Context To Read First

- `context/architecture-context.md` (Environments, Deployment Targets)
- `research/week-05-deployment-devops.md`
- `context/code-standards.md` (secrets, `.env.example`)

## Files Owned

- `backend/Dockerfile`
- `docker-compose.yml` (finalized service set: api + sqlserver + postgres + redis + mcp)
- `docs/DEPLOYMENT.md` (per-app runbooks; host matrix)
- `web/vercel.json` (optional Vite build/output overrides)
- `.env.example` (final per-environment variable sets)

## Files

CREATE: `backend/Dockerfile` — multi-stage: `dotnet/sdk:8.0` build → `dotnet/aspnet:8.0` runtime, `ENV ASPNETCORE_URLS=http://+:8080`, `EXPOSE 8080`, non-root run.
CREATE: `docs/DEPLOYMENT.md` — per-app runbooks: Vercel import steps, Railway service + volume + release-command (migrations), Trigger deploy, APK release; `.env` matrix for local/Railway/Vercel/Trigger.
MODIFY: `docker-compose.yml` — add `mcp` service (Streamable HTTP); keep parity with prod topology.
MODIFY: `.env.example` — final variable sets per environment.

## Setup / Initialization

```bash
# Backend image build + local run (the exact artifact Railway uses)
cd backend
docker build -t griot-api .
docker run --rm -p 8080:8080 --env-file ../.env griot-api   # then curl /health

# Web: push web/ to GitHub → Vercel import → select "Vite" preset → add VITE_API_URL → Deploy
# Railway: new project from GitHub → deploy from Dockerfile → set envs → set release command:
#   dotnet tool restore && dotnet ef database update
# MCP: compose `mcp` service pushed as its own Railway service
# AI: `npx trigger.dev@latest deploy` from ai/ (Trigger cloud dashboard)
# Mobile: APK artifact from Feature 08 CI run → install/release
```

## Separation of Concerns

- One deployment path per app; no shared runtime, no shared secrets across apps beyond the API's explicit env contract.
- Infrastructure-as-code stays minimal and visible: the compose file + one Dockerfile + documented runbooks. No invisible click-ops without a doc line.
- Host matrix documented (Railway primary, Render fallback, Azure App Service variant) — switching hosts changes only the runbook, never app code.

## Docker & Deploy

| App | Artifact | Host | Release command / step |
|---|---|---|---|
| backend | Docker image `griot-api` | Railway | `dotnet ef database update` (release cmd) |
| web | `dist` (Vite) | Vercel | Vercel build (Vite preset) |
| mcp | Docker image (Streamable HTTP) | Railway | container start |
| ai | Trigger.dev deployment | Trigger cloud | `trigger.dev deploy` |
| mobile | APK/AAB | CI artifact → Play/APK | CI build job |

Health checks: backend `/health`; web via Vercel; mcp via its HTTP endpoint. `docker compose up` locally still reproduces api + sqlserver + postgres + redis + mcp.

## Out of Scope

- Kubernetes, multi-region, autoscaling policies, zero-downtime blue/green (noted for v2).

## Acceptance Criteria

- [ ] Backend image builds and runs locally; `/health` OK; same image deploys on Railway
- [ ] Web live on Vercel (Vite preset) talking to the deployed API
- [ ] MCP live on Railway as Streamable HTTP; AI agents scheduled in Trigger cloud
- [ ] APK artifact downloadable; installs on the physical device
- [ ] `docs/DEPLOYMENT.md` complete; `.env.example` matches local + every host; `docker compose up` = full local topology

## Future Modifications

- Feature 10's Week-7 manual/UAT cycle runs against these deployed environments.