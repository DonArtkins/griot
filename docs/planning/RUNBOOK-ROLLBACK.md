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
2. Redeploy the last-good version via `npx trigger.dev@4.0.0 deploy` from `ai/` (version is pinned in `ai/package.json` devDependencies — use the exact version recorded there, not `@latest`).

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
  4. **Future enhancement (if RPO >24h unacceptable):** Enable PostgreSQL PITR with WAL archiving to external storage (requires setup, see `docs/planning/CODERABBIT-REMAINING-ISSUES.md` §2.2).
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