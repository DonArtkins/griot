# Progress Tracker — DevOps / Infra

## Current State

**Phase P4** in `docs/planning/IMPLEMENTATION-ROADMAP.md`. Kit written (7 specs). **Not started.** The phase runs after **P3 (mobile)** so that CI (spec 05) can build, test, and deploy every surface — backend, web, mobile APK — in one pass, and so Vercel (spec 01) deploys a feature-complete web app.

| Spec | Title | Status | Blocked by |
|---|---|---|---|
| 01 | Frontend deployment (Vercel) | Pending (P4 entry point) | web 01 ✅-capable (buildable app) — scheduled P4 |
| 02 | Backend Dockerization | Pending | backend code ✅ |
| 03 | Docker Compose: api + sqlserver | Pending | infra 02 |
| 04 | Build & orchestrate full local topology | Pending | infra 03 |
| 05 | GitHub Actions CI/CD | Pending | infra 04; wire web + mobile test jobs |
| 06 | Docker Hub push + Railway deploy | Pending | infra 01–05 |
| 07 | Netdata monitoring | Pending | infra 02 + 06 — **Phase-1 optimization gate: must ship before public launch** |

## Roadmap Order (canonical, from IMPLEMENTATION-ROADMAP.md P4)

`infra 01 → 02 → 03 → 04 → 05 → 06 → 07`

**Why infra after web + mobile:** CI/CD is only meaningful when there is something to build/deploy for every surface (backend xUnit, web Jest, Flutter). **Why before MCP:** mcp 04 (Railway deploy) consumes infra 02–04/06 — finishing infra first lets the entire MCP phase (P5) run against a deployed, stable API. Infra 01 (Vercel) could technically run right after web 06/07 are buildable, but the one-spec-at-a-time rule keeps it in P4.

## Next Steps

1. Do not start until P3 (mobile 01–07) is complete.
2. Then branch `feature/infra/01-frontend-deployment-vercel` and implement spec 01 only.
3. Verification gates: `docker compose up` reproduces the full local topology with health checks; CI green; `.env.example` matches local + every host; migrations run as release command.

## Session Notes

- **2026-09-03** — Infra kit created (AGENTS, skills, contexts, 7 specs).
- **2026-09-10 (2)** — Tracker created during the cross-system audit (this system previously had none; root tracker also said "6 feature specs" — there are 7). Phase P4 position + Netdata Phase-1 gate recorded above.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
