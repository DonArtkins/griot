# Progress Tracker — AI Agents (Trigger.dev v4) [own-stack]

## Current State

**Phase P2** in `docs/planning/IMPLEMENTATION-ROADMAP.md`. Kit written (15 specs). **Not started; backend spec 09 prerequisite is met** (`GRIOT_SERVICE_TOKEN` plus `X-On-Behalf-Of`, HMAC `/api/webhooks/trigger`). Client implementations remain planned. Roadmap position: start after **P1 (web 01–09)** completes.

| Spec | Title | Status | Blocked by |
|---|---|---|---|
| 01 | Trigger.dev setup + scaffold | Pending (P2 entry point) | P1 completion; backend 09 ✅ |
| 02 | Copilot agent + realtime streaming | Pending | ai 01, backend 05 ✅ + 09 ✅ |
| 03 | Scheduled agents (dueReminders/sprintDigest/staleBoard/standupBuilder) | Pending | ai 02, backend notifications (04/16 ✅) |
| 04 | Propose-before-write workflow | Pending | ai 02, **web 10** |
| 05 | Golden transcripts + cost budgets | Pending | ai 01–04 |
| 06 | Copilot knowledge agent + system auditor | Pending (**P2 wave**) | ai 02/05, backend 16/20/24 |
| 07 | Report generation (PDF + CSV) | Pending (**P2 wave**) | ai 02/03/05, backend 11/24 |
| 08 | Advanced executor (Level-4 planning loop) | Pending (**P2 wave**) | ai 02/04/05/06/07, backend 09/20/23/24 |
| 09 | BI Copilot, threads and capabilities | Pending | backend 25/26, ai 06/07 |
| 10 | Incident summaries and confirmed notices | Pending | backend 27, ai 09, web 12 |
| 11 | Institutional memory and pattern detection | Pending | backend 26/28, ai 09, web 12 |
| 12 | Explainable assignment suggestions | Pending | ai 11, web 12; human write path |
| 13 | Client support agent + client boundary | 📋 Spec written (PLANNED) — multi-tenant wave | ai 02/05/09, backend 25/26/29/34/22 |
| 14 | Handoff user-manual generator | 📋 Spec written (PLANNED) — multi-tenant wave | ai 02/03/05/07, backend 20/24/11/29/35 |
| 15 | Maintenance-phase triage agent | 📋 Spec written (PLANNED) — multi-tenant wave | ai 02/05/09/11/14, backend 20/22/25/26/29/35 |

## Next Steps

1. Do **not** start until backend 09 is ✅ and P1 (web 01–09) is complete.
2. Then branch `feature/ai/01-trigger-setup` and implement spec 01 only.
3. Verification gates: `npm run lint && npm run typecheck && npm test` green (mocked LLM, no network in CI).

### Roadmap order

`ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12`

**Why web 10 sits inside this phase (circular-dependency resolution):** web 10 (Copilot panel) needs ai 02's realtime stream, while ai 02's spec lists "web feature 10" — that reference is a *contract-design* dependency (the panel is the stream's consumer), not a build-order one. ai 04 genuinely needs the panel's approval cards, so web 10 must land before ai 04. Build ai 01–02 first, then web 10, then continue ai 03 → 04 → 05 → 06 → 07 → web 11 → ai 08 → 09 → web 12 → ai 10 → 11 → 12 (canonical P2 in `docs/planning/IMPLEMENTATION-ROADMAP.md`).

## Session Notes

- **2026-09-12 (tracker repair)** — Corrected current counts, table structure, and prerequisite status; kept historical checkpoints inside Session Notes. Backend 29–31 are implemented; backend 32 is next. This system's features remain pending; tenant extensions follow their owning specs' dependencies.

- **2026-09-11 (superpowers wave sync)** — Added PLANNED specs ai 06 (knowledge agent + system auditor), ai 07 (report generation PDF + CSV) and ai 08 (advanced Level-4 executor) to the P2 order; web 11 (Reports & Audit Center) sits between ai 07 and ai 08. Boundaries codified: reports/audit ride the scoped `CreateReport` capability (backend 24); auth/OTP is human-only (backend 23, AI OBO 403). No production code; contract-sync run.

- **2026-09-10 (backend 09 sync)** — Backend OBO authentication is delivered: planned AI clients must send the service token and an authorized real-user `X-On-Behalf-Of` identity, including scheduled runs. Architecture and setup spec record the four-scope contract. No AI production code was implemented; P2 still waits for P1. Backend verification: 71 SQL-enabled tests passed with no skips/failures after the approved historical-fixture repair.

- **2026-09-03** — AI kit created (AGENTS, skills, contexts, 5 specs).
- **2026-09-10** — Trigger.dev orchestration contract ratified (`research/ai-integration.md` §2a): Trigger.dev is a compute/orchestration adapter, never a data owner; the .NET backend is the only task trigger and the only writer of source-of-truth data; web/mobile never call Trigger.dev (exception: read-only Copilot realtime stream).
- **2026-09-10 (2)** — Tracker created during the cross-system audit (this system previously had none, violating the contract-sync + git-branch-flow requirement of one tracker per system). Phase P2 position + the web10↔ai02 cycle resolution recorded above.

- **2026-09-11 (multi-tenant wave sync)** — **Multi-Tenant Migration Wave (PLANNED):** specs 01–12 each gained a "Multi-Tenant Update (2026-09-11 — PLANNED)" section (org-stamped payloads/threads/memory, per-org scheduling + `budget:org:{orgId}:…` budgets, org+role-context proposals, per-org reports + client progress variant, per-org executor bounds, SuperAdmin-delegated platform ops agent, org-scoped memory reuse, role/permission-aware suggestions respecting custom roles). New specs **13** (client support agent + client boundary), **14** (handoff user-manual generator via spec 20 outbox) and **15** (maintenance-phase triage) written as PLANNED, plus the client capability tier (backend 25 extension). No production code; no status changes to existing specs. Contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`; AI multi-tenant contract mirrored in `ai/project-kit/context/stack-contract.md`.

### Audit synchronization — 2026-09-11 (historical)

Implemented through backend 20 (observability pipeline); backend 29 (multi-tenant foundation) implemented 2026-09-11 on `feature/backend/29-multi-tenant-foundation-organizations` — roadmap §P0.5 next is backend 30 after 29 completes its outstanding gates. Future planning is not completed implementation. P0 (2026-09-11): backend 29 ✅ → 30 ✅ → 31 ✅ → 32 → 33 → 34 → 35 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10. P2: ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12. Full requirement/review ledger: `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`.
AI kit now has 12 planned specs. Institutional memory is ai 11; assignment suggestions ai 12. No direct SQL, auto-commit, silent assignment or borrowed mutation scopes.

### CodeRabbit follow-up — 2026-09-11 (historical)

Review corrections are documented in `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`: backend 24/28 schema proposals, backend 27 incident/delivery boundaries, notification availability and final documentation dependencies are synchronized. AI 01 pins the existing Trigger.dev v4 decision to 4.5.16. Future features remain PLANNED. The user authorized this one-time review-batch grouping and commit/push on 2026-09-11 ("COMMIT AND PUSH TO GITHUB" in response to the exception request). Build, 108 SQL-enabled tests, API health, clean npm install/imports and contract-sync passed; details are recorded in the ledger. Future features remain on separate branches/PRs; backend 20 starts only after backend 09 review approval.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
