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

## Where This System Sits in the Build Order (canonical: `docs/planning/IMPLEMENTATION-ROADMAP.md`)

**Phase P4 (Week 5 system)** — after web (P1) + AI hop (P2) + mobile (P3), so CI (spec 05) can build/test/deploy every surface in one pass. Own order: **01 → 02 → 03 → 04 → 05 → 06 → 07**; spec 07 (Netdata) is a Phase-1 optimization gate — it must ship before public launch, not be deferred to QA. MCP (P5) consumes infra 02–04/06. Entry branch: `feature/infra/01-frontend-deployment-vercel`. Track state in `infra/project-kit/context/progress-tracker.md`.

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


## Multi-Tenant Migration Wave (2026-09-11 — PLANNED)

Griot becomes a multi-tenant platform (canonical contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`; owner specs: backend 29–35). Infra impact — all PLANNED, **no new service/container**:

- **New env vars (secret-store only, never committed):** `JWT__Key` (production ≥ 64 chars / 512 bits, CSPRNG — backend 30 policy), `SUPERADMIN__EMAIL`, `SUPERADMIN__PASSWORD` (SuperAdmin bootstrap, backend 30/32), `Organizations:RetentionDays` (default 30 — offboard purge window, backend 33). Compose `.env.example`, Railway variables and CI secrets carry them when infra 02/03/05/06 ship their bumps.
- **Topology unchanged:** Pool-model tenancy is schema-level (`OrganizationId` columns, backend 29/36); no per-tenant containers, databases or volumes. Suspend/offboard behavior is backend-owned; compose carries env parity only.
- **Migrations:** the Railway release command picks up `AddMultiTenantColumns` (backend 29) unchanged.
- **PITR unchanged and platform-level:** a restored service restores ALL tenants atomically; the per-org export bundle (backend 33) is the tenant-level data path, not a restore mechanism.
- **CI:** the cross-tenant isolation suite (qa 14) joins the pipeline as a blocking gate; per-tenant Netdata dashboards are Phase 2, evidence-gated (infra 07).
- Implemented-status claims above are unchanged until each bump ships on its own feature branch.

## Historical notes

These dated snapshots preserve prior decisions. Current work and verification are recorded in the owning progress tracker.

### Audit synchronization — 2026-09-11

Implemented through backend 20 (observability pipeline); backend 29 (multi-tenant foundation) implemented 2026-09-11 on `feature/backend/29-multi-tenant-foundation-organizations` — roadmap §P0.5 next is backend 30 after 29 completes its outstanding gates. Future planning is not completed implementation. P0 (2026-09-11): backend 29 ✅ → 30 → 31 → 32 → 33 → 34 → 35 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10. P2: ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12. Full requirement/review ledger: `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`.
No new service/container is required. SQL Server remains primary; memory does not add a vector database. Trigger cloud only. Backend grants are provisioned privately; no static shared stream token in a frontend environment variable.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
