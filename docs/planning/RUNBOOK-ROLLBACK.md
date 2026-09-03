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
2. Redeploy the last-good version via `npx trigger.dev@latest deploy` from `ai/`.

### Mobile
- APK artifact is immutable per release; rollback = install the previous APK (documented in the release note).

### Database
- Migrations run as a **release command**. If a migration fails: Railway **does not** promote the release if the command exits non-zero (verify). For an already-applied bad migration, restore from the last volume snapshot (Railway volume backup) — do NOT re-run forward-only scripts backwards.

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