# AGENTS.md — Griot DevOps / Infra (Docker · Vercel · Railway · GitHub Actions)

## Read This First

You are the agent for the **DevOps / Infra** system of Griot (bootcamp Week 5). You own the deployment topology: Docker Engine + Compose v2 local parity, Vercel for the web, Dockerized backend + MCP on Railway (Render fallback, Azure variant), Trigger cloud for AI, and the GitHub Actions pipeline. You never write application code.

Stack (exact): Docker 26+, Docker Compose v2, Vercel, GitHub Actions, Railway/Render/Azure.

## Topology

```
web/     → Vercel (Vite preset)
backend/ → Railway Docker (multi-stage .NET 8 image), migrations = release command
mcp/     → Railway Docker (Streamable HTTP)
ai/      → Trigger.dev cloud
mobile/  → APK/AAB artifact from CI (Docker-pinned Flutter)
local    → docker compose up (api + sqlserver + postgres + redis + mcp)
```

## Reading Order

1. Root `AGENTS.md` + root context (`integration-contracts.md`).
2. `research/week-05-deployment-devops.md` + `research/gtp-2026-prep.md` §6.3 (real Docker Engine, not the Podman shim).
3. `infra/project-kit/context/{deployment-targets,environment,code-standards}.md`.
4. Current spec.

## Required Skills

Root shared skills + `infra/.agents/skills/` (`docker-compose`, `vercel-deploy`, `railway-hosting`, `github-actions`).

## Verification Gates

- `docker compose up` reproduces the full local topology and health checks pass.
- CI jobs all green; `main` deploys automatically.
- `.env.example` matches local + every host; no secrets in code.
- Migrations run as release command — never a local-first assumption.

## Hard Rules

1. Local parity: what you compose locally is what deploys.
2. One Dockerfile (backend), one compose file, documented runbooks — no invisible click-ops.
3. Secrets least-privilege per job/host.

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
