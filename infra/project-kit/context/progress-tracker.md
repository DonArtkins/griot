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

- **2026-09-11 (multi-tenant wave sync)** — PLANNED wave per `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`: specs 01–07 each gained a "Multi-Tenant Update (2026-09-11 — PLANNED)" section (env parity: `JWT__Key` ≥ 64 chars, `SUPERADMIN__EMAIL/PASSWORD`, `Organizations:RetentionDays`; `AddMultiTenantColumns` via the release command; qa 14 isolation suite in CI; PITR unchanged — platform-level restore with backend 33 exports as the per-org path; per-tenant Netdata dashboards Phase 2 evidence-gated). No new spec, service, container or Railway variable mapping; existing statuses untouched.
- **2026-09-03** — Infra kit created (AGENTS, skills, contexts, 7 specs).
- **2026-09-10 (2)** — Tracker created during the cross-system audit (this system previously had none; root tracker also said "6 feature specs" — there are 7). Phase P4 position + Netdata Phase-1 gate recorded above.

## Audit synchronization — 2026-09-11

Implemented through backend 20 (observability pipeline); backend 29 (multi-tenant foundation) implemented 2026-09-11 on `feature/backend/29-multi-tenant-foundation-organizations` — roadmap §P0.5 next is backend 30 after 29 completes its outstanding gates. Future planning is not completed implementation. P0 (2026-09-11): backend 29 ✅ → 30 → 31 → 32 → 33 → 34 → 35 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10. P2: ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12. Full requirement/review ledger: `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`.
No new service/container is required. SQL Server remains primary; memory does not add a vector database. Trigger cloud only. Backend grants are provisioned privately; no static shared stream token in a frontend environment variable.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
