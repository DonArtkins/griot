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

## Implemented authentication contract (Feature 07)

Use the [auth contract](../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

## Database recovery (restored-service cutover)

Recovery contract: `POSTGRES_RECOVERY_TARGET_TIME` (set automatically on the restored
`<service>-restored-YYYYMMDD-HHMM` Railway PostgreSQL service), read-only source WAL replay,
validate-then-quiesce-then-cutover via `ConnectionStrings__Default` — distinct from normal SQL Server
configuration. PITR must be enabled before an incident (first post-enable base backup complete). See
`docs/planning/RUNBOOK-ROLLBACK.md` + infra spec 06.

Before committing or pushing implementation, run `python3 scripts/check-contract-sync.py` from
the repository root. Synchronize the owning spec, dependent specs, planning,
research, docs, contexts, agent instructions, diagram sources and progress notes
in the feature branch. Planned behavior must be labeled and must not count as
implemented acceptance evidence. Run the system verification gates as well.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
