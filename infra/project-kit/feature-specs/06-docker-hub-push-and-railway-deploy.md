# Feature 06 - Docker Hub Push + Railway Deploy

## Type

NEW FEATURE (Optional Advanced Deployment per the PDF)

## What This Delivers

The PDF's optional lines "Push image to Docker Hub" and "Deploy to Azure / Railway / Render" realized as: registry push + Railway deployment of backend and MCP, with Render + Azure variants documented.

## Dependencies

- Features 01-05 (image + CI exist).

## Context To Read First

- `research/week-05-deployment-devops.md` sec 6
- `infra/project-kit/context/deployment-targets.md`

## Agent Skills To Use

- `infra/.agents/skills/railway-hosting/SKILL.md`

## Files Owned

- `docs/DEPLOYMENT.md`, `docs/RAILWAY.md`, `docs/RENDER.md`, `docs/AZURE.md`

## Files

CREATE: per-app runbooks + env matrix; Railway backend service with release command `dotnet tool restore && dotnet ef database update`; Railway mcp service; optional Docker Hub push step in CI.

## Setup / Initialization

```bash
railway up --service griot-api
railway variables --service griot-api --set 'ConnectionStrings__Default=...'  # etc.
# release command set in Railway service settings, NOT in CI
```

## Implementation Notes

- Railway release command runs migrations against production SQL.
- Render fallback + Azure App Service variant (az webapp up) documented but not defaulted.

## Database Recovery & Restored-Service Cutover

Operational recovery contract (full runbook: `docs/planning/RUNBOOK-ROLLBACK.md`):

- Railway managed PostgreSQL PITR (pgBackRest) provisions an independent restored service named `<service>-restored-YYYYMMDD-HHMM` with its own NEW volume; env vars are copied from the source EXCLUDING archive credentials.
- `POSTGRES_RECOVERY_TARGET_TIME` is set automatically on the restored service (the chosen recovery timestamp, which must fall within Railway's PITR retention window).
- The restored service reads the source WAL archive in read-only mode and executes `pgbackrest restore --type=time --target=<timestamp>`; the source DB is never written by the recovery.
- **Validate before cutover:** check last `AuditLogs`/`ActivityLogs` timestamp vs the recovery target, then run smoke tests against the restored connection string.
- **Quiesce dependent-service writes** (maintenance/read-only mode) before switching `ConnectionStrings__Default` to the restored service, or document reconciliation of post-target writes — writes to the source after `POSTGRES_RECOVERY_TARGET_TIME` are NOT in the restored fork and are lost on cutover.
- **Cutover** = update `ConnectionStrings__Default` on dependent Railway services (Vercel/Trigger env in the same step) and redeploy.
- This recovery flow is **DISTINCT from normal SQL Server `ConnectionStrings__Default` configuration**; it applies only during a PITR recovery incident.
- PITR must already be enabled with its first post-enable base backup complete — enabling it after an incident does NOT create a historical restore window.
- **Self-managed (non-Railway) PostgreSQL:** recovery settings live in `postgresql.conf` (NOT `recovery.conf`, removed in PostgreSQL 12); targeted PITR = empty `recovery.signal` in the data directory + `recovery_target_time` in `postgresql.conf`.

## Separation of Concerns

- Host choices documented; app code host-agnostic.

## Docker & Deploy

- Completes the Week-5 deploy matrix.

## Out of Scope

Kubernetes, autoscaling (v2).

## Acceptance Criteria

- [ ] Backend live on Railway; REST + GraphQL + health responding; migrations by release command
- [ ] MCP live on Railway (Streamable HTTP)
- [ ] Runbooks complete (Railway primary, Render fallback, Azure variant)


## Multi-Tenant Update (2026-09-11 — PLANNED)

- `JWT__Key` (≥ 64 chars, CSPRNG, secret-store; rotation runbook in backend 30) and `SUPERADMIN__EMAIL`/`SUPERADMIN__PASSWORD` are Railway variables on the backend service — never committed; `Organizations__RetentionDays` default 30 (backend 33).
- PITR unchanged and **platform-level**: a restored `<service>-restored-YYYYMMDD-HHMM` service restores ALL tenants atomically (tenancy is schema-level `OrganizationId` columns); no per-tenant restore exists — the per-org data path is the backend 33 export bundle.
- Per-org export/restore drill note: periodically validate that an offboard export (backend 33) can be downloaded and inspected as the tenant-level "restore" path; PITR remains the incident mechanism.
- Railway release command unchanged; migrations now include `AddMultiTenantColumns` (backend 29).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
