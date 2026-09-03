# Feature 08 — CI/CD: GitHub Actions Pipeline

## Type

NEW FEATURE

## What This Delivers

The GitHub Actions pipeline that gates every PR (build + test every app) and deploys on `main` (backend → Railway, web → Vercel, mcp → Railway, ai → Trigger, mobile → APK artifact). One workflow with per-app jobs — the Week-5 "test gate → deploy" and the Week-7 "CI/CD-integrated automated testing" outcomes built early.

## Dependencies

- Feature 04 (backend), 05 (web), 06 (mobile), 07 (ai/mcp) — each has at least a first passing slice.
- Feature 01 (branch protection on `main`).

## Context To Read First

- `context/test-validation-plan.md`
- `research/week-05-deployment-devops.md`
- `context/code-standards.md` (verification gates)

## Files Owned

- `.github/workflows/ci-cd.yml`
- `.github/workflows/*.yml` (if split)
- Secrets configuration reference (documented, values live in GitHub UI)

## Files

CREATE: `.github/workflows/ci-cd.yml`:
- `test` job: services − SQL Server 2022 container (`mcr.microsoft.com/mssql/server:2022-latest`, `ACCEPT_EULA:Y`, test password; ports 1433). Steps: checkout → setup-dotnet 8.0.x → restore → build → `dotnet test --collect:"XPlat Code Coverage"`.
- `web` job: Node 20 → `npm ci` → `npm run lint && npm run typecheck && npm test && npm run build`.
- `mobile` job: Docker-pinned Flutter image → `flutter analyze` → `flutter test` → build release APK → upload artifact.
- `ai`/`mcp` jobs: Node 20 each, own `npm ci` + lint/typecheck/test (golden transcripts, MCP contracts — no LLM network).
- `newman` job: run the Postman collection against the CI-deployed API.
- `cypress` job: E2E core loop with the MSW-stubbed copilot.
- `deploy` job: `needs: [test, web, mobile, ai, mcp, newman, cypress]`, `if: github.ref == 'refs/heads/main'` → trigger Vercel + Railway + Trigger deploys.

CREATE: `.github/workflows/README.md` — how gates are enforced, how to add a deploy, secrets list.

## Setup / Initialization

```bash
mkdir -p .github/workflows
# Repository secrets in GitHub UI:
#   RAILWAY_TOKEN, VERCEL_TOKEN + ORG/PROJECT id, TRIGGER_API_KEY,
#   GRIOT_SERVICE_TOKEN, POSTMAN_API_KEY (optional), CODECOV_TOKEN (optional)
```

No local action required beyond pushing the workflow — CI is the first consumer of every repo change.

## Separation of Concerns

- One job per app, one concern per job: **build/test is never mixed with deploy**; deploy is a separate job with `needs`.
- Test jobs run in parallel; deploy waits for all gates.
- CI service containers are ephemeral — the workflow must never depend on a developer's local `docker compose`.
- Secrets are per-job scoped (least privilege): the Vercel job cannot read the Trigger key.

## Docker & Deploy

- The `test` job boots SQL Server as a **service container** (the same image family used in Feature 02) — this is the parity check that CI and local agree.
- Deploys: Vercel via the GitHub integration (web), Railway via `railway up`/API (backend + mcp), Trigger via `trigger.dev deploy` (ai), APK artifact (mobile).
- Migrations run as the Railway release command — never as a CI `dotnet ef database update`.

## Out of Scope

- Multi-environment (staging/preview) promotion, auto-rollback, dynamic matrix generation.

## Acceptance Criteria

- [ ] Every PR runs all jobs; any red job blocks merge
- [ ] `main` merges trigger automated deploys for web/backend/mcp/ai
- [ ] Mobile APK downloadable from triggered run
- [ ] Newman + Cypress jobs green against the deployed API
- [ ] Workflow README documents the gates and secrets

## Future Modifications

- Feature 10 hardens coverage thresholds + OWASP scanning steps.