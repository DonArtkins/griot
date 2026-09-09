# Deployment Targets

| App | Artifact | Host | Release step |
|---|---|---|---|
| backend | Docker image (multi-stage .NET 8, port 8080) | Railway (primary) · Render (fallback) · Azure App Service (variant) | `dotnet ef database update` as release command |
| web | `dist` (Vite build) | Vercel (Vite preset) | Vercel build/deploy via GitHub integration |
| mcp | Docker image (Streamable HTTP, port 3001) | Railway | container start |
| ai | Trigger.dev deployment | Trigger cloud (or self-hosted Railway) | `trigger.dev deploy` |
| mobile | APK/AAB | CI artifact → Play/APK | Docker-pinned Flutter build job |

Host matrix rationale (from research): Railway chosen as Docker-native + one-click DB add-ons; Render as fallback; Azure documented as an optional stretch (`az webapp up`).

## Database recovery (restored-service cutover)

Recovery contract: `POSTGRES_RECOVERY_TARGET_TIME` (set automatically on the restored `<service>-restored-YYYYMMDD-HHMM` Railway PostgreSQL service), read-only source WAL replay, validate-then-quiesce-then-cutover via `ConnectionStrings__Default` — distinct from normal SQL Server configuration. PITR must be enabled before an incident (first post-enable base backup complete). See `docs/planning/RUNBOOK-ROLLBACK.md` + infra spec 06.

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
