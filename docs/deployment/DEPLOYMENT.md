# DEPLOYMENT — Griot (per-app runbooks)

> The deploy matrix per app. Detailed runbooks live in `infra/project-kit/`; this is the root index + rollback pointers. See also `docs/planning/RUNBOOK-ROLLBACK.md`.

| App | Artifact | Host | Deploy | Rollback |
|---|---|---|---|---|
| web | Vite `dist` | Vercel (Vite preset) | GitHub → Vercel import → env → deploy | Vercel promote previous deployment |
| backend | Docker image (port 8080) | Railway | Git push + release command (see Migrations rule) | Railway rollback previous deploy (see RUNBOOK-ROLLBACK.md) |
| mcp | Docker image (Streamable HTTP 3001) | Railway | separate service | Railway rollback |
| ai | Trigger.dev deploy | Trigger cloud | `npx trigger.dev@3 deploy` (version pinned in `ai/package.json` devDependencies — see `ai/project-kit/feature-specs/01-trigger-setup.md`) | Deactivate task / redeploy |
| mobile | APK/AAB | CI artifact → Play/APK | Docker-pinned Flutter build | install previous APK |

## Env matrix (per app)

- backend: `ConnectionStrings__Default`, `JWT__SigningKey`, `JWT__Issuer`, `JWT__Audience`, `Redis__Connection`, `GRIOT_SERVICE_TOKEN`, `Cors__AllowedOrigins`
- web: `VITE_API_URL`
- ai/mcp: `GRIOT_API_URL`, `GRIOT_SERVICE_TOKEN` (+ ai: LLM keys, `TRIGGER_WEBHOOK_SECRET`)
- compose (local): `GTP_SA_PASSWORD`, `GTP_PG_PASSWORD`

## Local parity

`docker compose up` runs api + sqlserver + postgres + redis + mcp — the exact topology that deploys. Deployment day is never the first time the wiring runs.

## Migrations rule

EF migrations run as the **Railway release command** (never a local-first assumption). A failing release blocks promote.

**Critical**: All schema migrations must be expand/contract compatible to support application rollback without database rollback:
- **Expand phase**: Add new columns/tables as nullable; old code ignores them, new code uses them.
- **Contract phase**: Remove old columns/tables only after all deployments use the new schema.
- Single-release breaking changes (column drop, type change, NOT NULL on existing column) require coordinated blue-green deployment or maintenance window.
- Release command: `dotnet ef database update` (applies forward migrations only).
- See `docs/planning/RUNBOOK-ROLLBACK.md` for tested database recovery procedure (backup restore, RPO checks).

## Verify after any deploy

- [ ] `GET /health` 200 (backend)
- [ ] Web loads; talks to deployed API (CORS + env correct)
- [ ] MCP Streamable HTTP responds
- [ ] AI scheduled task runs (Trigger dashboard)
- [ ] Rollback path reachable (above)

---
**Engineering Excellence. Production Mindset. Professional Impact. 🚀**