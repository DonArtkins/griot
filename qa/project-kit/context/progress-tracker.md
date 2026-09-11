# Progress Tracker - QA

## Current State

**Phase P6** in `docs/planning/IMPLEMENTATION-ROADMAP.md` (Weeks 6–7). Kit written (13 specs). **Not started.** QA verifies the other six systems, so it runs last — but specs 01–03 are process docs with zero code dependencies (usable as gap fillers any time), and spec 04's base collection already exists (backend 08 ✅).

| Spec | Title | Status | Blocked by |
|---|---|---|---|
| 01 | QE fundamentals doc | Pending (no code deps — gap filler) | — |
| 02 | Manual testing excellence | Pending (gap filler) | — |
| 03 | Test management in Jira | Pending (gap filler) | — |
| 04 | API testing mastery (Postman) | Pending | backend 08 ✅ (base collection); folder 14 strict mode waits on backend 20 (PLANNED — not yet implemented) |
| 05 | Newman + contract testing | Pending | qa 04, **infra 05** (CI jobs) |
| 06 | xUnit (.NET) suites | Pending | backend 04–07 ✅ |
| 07 | Jest + RTL suites | Pending | web code (P1) |
| 08 | Flutter testing | Pending | mobile code (P3) |
| 09 | Cypress E2E | Pending | web app shell (w 07) |
| 10 | Perf (k6) + OWASP + a11y | Pending | deployed topology (P4) |
| 11 | DevOps/TDD >80% coverage gate | Pending | all code (P0–P4), infra 05 |
| 12 | Manual cycles + defects + internal QE | Pending | deployed system |
| 13 | Perf benchmarks + UAT + exec report | Pending | everything deployed |
| 14 | Multi-tenant isolation suite | 📋 Spec written (PLANNED) — multi-tenant wave | backend 29–35, qa 04–06/10, infra 05 |

## Roadmap Order (canonical, from IMPLEMENTATION-ROADMAP.md P6)

`qa 01 → 02 → 03 → 04 → 05 → 06 → 07 → 08 → 09 → 10 → 11 → 12 → 13`

**Why QA is last:** it gates the merge of every other system and needs the deployed Railway/Vercel topology for manual + UAT cycles (qa 12–13). Nothing downstream waits on QA, but everything upstream must exist for QA's coverage gates (≥80% service layer + auth) to be honest.

## Next Steps

1. Wait for P5 (mcp) to close, or run qa 01–03 in any idle gap — they have no code dependencies.
2. Then branch `feature/qa/01-qe-fundamentals` (or the next qa spec in sequence) — one spec at a time.
3. Verification gates: xUnit, Jest+RTL, Flutter, Cypress, Newman green; coverage ≥80%; k6 baseline recorded; OWASP logged.

## Session Notes

- **2026-09-11 (multi-tenant wave sync)** — PLANNED wave per `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`: specs 01–13 each gained a "Multi-Tenant Update (2026-09-11 — PLANNED)" section (multi-tenant manual/Jira matrix in 02/03; org routes + JWT v2 claims + negative cross-tenant tests in 04/05; isolation integration tests in 06; org switcher + client-portal Jest tests in 07; Flutter org-switch tests in 08; multi-tenant E2E journeys in 09; k6 multi-tenant + OWASP IDOR/IDOT in 10; `IPermissionService` coverage in 11; multi-tenant UAT + evidence in 12/13) and NEW spec 14 (multi-tenant isolation suite) was written. No production code; existing statuses untouched.
- **2026-09-10 (2)** — Layer-order audit (docs-only): tracker rebuilt with per-spec blockers; qa 01–03 identified as zero-code-dep gap fillers; qa 04's base collection already exists (backend 08 ✅); qa 05 pinned to infra 05. Canonical P6 order per `docs/planning/IMPLEMENTATION-ROADMAP.md`.
- **2026-09-03** - QA kit created.


## Audit synchronization — 2026-09-11

Implemented through backend 20 (observability pipeline); backend 29 (multi-tenant foundation) implemented 2026-09-11 on `feature/backend/29-multi-tenant-foundation-organizations` — roadmap §P0.5 next is backend 30 after 29 completes its outstanding gates. Future planning is not completed implementation. P0 (2026-09-11): backend 29 ✅ → 30 → 31 → 32 → 33 → 34 → 35 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10. P2: ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12. Full requirement/review ledger: `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`.
QA owns independent test evidence, not AI certification. Extend existing suites for template counts, >1,000 rows, phase eligibility, cross-role/source access, replay/idempotency, notices and human assignment approval.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
