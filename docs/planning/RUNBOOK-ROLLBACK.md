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
2. Redeploy the last-good version via `npx trigger.dev@3 deploy` from `ai/` (version is pinned in `ai/package.json` devDependencies — use the exact version recorded there, not `@latest`).

### Mobile
- APK artifact is immutable per release; rollback = install the previous APK (documented in the release note).

### Database
- **Expand/contract migrations**: All schema changes must support application rollback without database rollback. Rolling back the application image does NOT undo applied migrations.
- **Migration failure**: Railway does not promote the release if the migration command exits non-zero (the deployment is rejected). No manual intervention needed.
- **Post-deployment schema issue**: If an already-applied migration causes production issues:
  1. **Application rollback first**: Railway rollback to previous image (works if migration was expand-phase compatible).
  2. **Database recovery** (coordinated procedure):
     - Restore from last Railway volume snapshot (point-in-time backup).
     - Verify Recovery Point Objective (RPO): check `AuditLogs` / `ActivityLogs` for last recorded transaction timestamp vs backup timestamp.
     - Reapply any lost transactions if within acceptable RPO window, or document data loss.
     - Do NOT attempt to manually reverse forward-only migrations; use tested backup restore.
  3. **Communication**: Notify team and users of any data loss or downtime window.
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