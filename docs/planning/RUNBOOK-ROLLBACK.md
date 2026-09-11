# Runbook / Rollback — Griot

> One page: if a deploy breaks production, the exact command sequence to revert, and who/what notices before a user reports it.

## Deploy did NOT go well — revert now

### Web (Vercel)
1. Open Vercel → project → **Deployments**.
2. Find the last **good** deployment → **⋯ → Promote to Production** (instant). 
3. (Alternative) Git revert + push → Vercel rebuilds main.

### Backend / MCP (Railway)
1. Railway → service → **Deployments** tab.
2. Click ⋯ on the previous **good** deployment → **Rollback** (Railway redeploys the old image/commit).
3. Verify `/health` returns 200.

### AI agents (Trigger.dev)
1. Trigger dashboard → the task/agent → **Deactivate** (stops scheduled runs).
2. Redeploy the last-good version via `npm run deploy` from `ai/` (version is pinned in `ai/package.json` devDependencies — use the exact version recorded there, not `@latest`).

### Mobile
- APK artifact is immutable per release; rollback = install the previous APK (documented in the release note).

### Database
- **Expand/contract migrations**: All schema changes must support application rollback without database rollback. Rolling back the application image does NOT undo applied migrations.
- **Migration failure**: Railway does not promote the release if the migration command exits non-zero (the deployment is rejected). No manual intervention needed.
- **Post-deployment schema issue**: If an already-applied migration causes production issues:
  1. **Application rollback first**: Railway rollback to previous image (works if migration was expand-phase compatible).
  2. **Database recovery** (Railway volume snapshots):
     - **Railway volume snapshots** are fixed-point backups taken at Railway-defined intervals (typically 24 hours)
     - **NOT arbitrary timestamp recovery (PITR)** — Railway snapshots restore to snapshot time only, not custom timestamps
     - **Recovery process:**
       1. Railway dashboard → Database service → Backups tab
       2. Select last good snapshot → Restore to new volume
       3. Update connection string to restored volume
       4. Verify RPO: Check last `AuditLogs`/`ActivityLogs` timestamp vs snapshot time
     - **RPO (Recovery Point Objective):** Time since last snapshot (typically 24 hours)
     - **Data loss:** Transactions between snapshot time and incident time are lost
     - **Transaction replay:** NOT feasible with current schema (AuditLogs contains state snapshots, not commands). Document data loss and coordinate with affected users.
  3. **Communication**: Notify team and users of data loss window and RPO.
   4. **Future enhancement (if RPO >24h unacceptable):** Use Railway's managed PostgreSQL PITR (pgBackRest-backed):
      - **Prerequisite:** PITR must already be enabled on the source PostgreSQL service **and** its first post-enable base backup must be complete. Enabling PITR after an incident does NOT retroactively provide a historical restore window — only timestamps after the first base backup are recoverable.
      - In Railway dashboard, select the PostgreSQL service → **Backups** → **Point-in-Time Recovery**
      - Choose a target recovery timestamp (must fall within Railway's available PITR retention window — approximately 4 weeks from the oldest retained full backup)
      - Railway provisions a NEW separate PostgreSQL service (typically named `<service>-restored-YYYYMMDD-HHMM`) with a new volume
      - Environment variables are copied from the source (excluding archive credentials); `POSTGRES_RECOVERY_TARGET_TIME` is set automatically
      - The restored service reads from the source WAL archive in read-only mode and executes `pgbackrest restore --type=time --target=<timestamp>`
      - Validate the restored database: check last `AuditLogs`/`ActivityLogs` timestamp, run smoke tests against the new connection string
      - **Quiesce writes before cutover:** The restored database is an independent fork — any writes to the source database after `POSTGRES_RECOVERY_TARGET_TIME` are NOT present in the restored service and will be lost on cutover. Before switching `ConnectionStrings__Default`, either (a) put dependent services in maintenance/read-only mode to stop new writes, or (b) document the data-loss window and coordinate reconciliation of post-target writes with affected users.
      - Cut over connections: update `ConnectionStrings__Default` on dependent Railway services and redeploy; update Vercel/Trigger env vars
      - **Self-managed PostgreSQL only (non-Railway):** Configure `archive_mode=on`, `archive_command`, and `restore_command` in `postgresql.conf` (NOT `recovery.conf`, which was removed in PostgreSQL 12 and prevents startup if present). For targeted PITR, create an empty `recovery.signal` file in the data directory and set `recovery_target_time` in `postgresql.conf`; the server removes `recovery.signal` automatically upon completing recovery. Test restore periodically against a staging instance.
- **Prevention**: Test migrations in staging environment with production-like data volume before deploying to production.

## What notices before a user does

1. **Uptime ping** (Better Uptime / UptimeRobot) → hits `https://<api>/health` every 60 s; alerts on non-200.
2. **Railway health check** on the API; deployment fails if `/health` doesn't pass.
3. **ErrorLogs table** + structured log stream (request ids) → alert on 5xx spike (error-tracking in `MONITORING.md`).
4. **k6 smoke** in CI on every deploy (qa) — catches regression before merge.

## Pre-deploy checklist (production launch day)

- [ ] `dotnet build && dotnet test` green; web/mobile/ai/mcp gates green; Newman + Cypress green in CI
- [ ] Migrations are the Railway release command (never local)
- [ ] `.env.example` matches Railway + Vercel + Trigger var sets
- [ ] `/health` + uptime ping live; ErrorLogs/ApiLogs recording
- [ ] Rollback paths documented above are reachable by the operator
- [ ] k6 baseline recorded (qa)

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**