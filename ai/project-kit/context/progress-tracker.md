# Progress Tracker — AI Agents (Trigger.dev v3) [own-stack]

## Current State

**Phase P2** in `docs/planning/IMPLEMENTATION-ROADMAP.md`. Kit written (5 specs). **Not started — hard-blocked by backend spec 09** (`GRIOT_SERVICE_TOKEN` + HMAC `/api/webhooks/trigger`): `ai/` has no other legal path to Griot data, so no AI spec can meet acceptance criteria before 09 exists. Roadmap position: start after **P1 (web 01–09)** completes.

| Spec | Title | Status | Blocked by |
|---|---|---|---|
| 01 | Trigger.dev setup + scaffold | Pending (P2 entry point) | backend 09 |
| 02 | Copilot agent + realtime streaming | Pending | ai 01, backend 05 ✅ + 09 |
| 03 | Scheduled agents (dueReminders/sprintDigest/staleBoard/standupBuilder) | Pending | ai 02, backend notifications (04/16 ✅) |
| 04 | Propose-before-write workflow | Pending | ai 02, **web 10** |
| 05 | Golden transcripts + cost budgets | Pending | ai 01–04 |

## Roadmap Order (canonical, from IMPLEMENTATION-ROADMAP.md P2)

`ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05`

**Why web 10 sits inside this phase (circular-dependency resolution):** web 10 (Copilot panel) needs ai 02's realtime stream, while ai 02's spec lists "web feature 10" — that reference is a *contract-design* dependency (the panel is the stream's consumer), not a build-order one. ai 04 genuinely needs the panel's approval cards, so web 10 must land before ai 04. Build ai 01–02 first, then web 10, then finish ai 03–05.

## Next Steps

1. Do **not** start until backend 09 is ✅ and P1 (web 01–09) is complete.
2. Then branch `feature/ai/01-trigger-setup` and implement spec 01 only.
3. Verification gates: `npm run lint && npm run typecheck && npm test` green (mocked LLM, no network in CI).

## Session Notes

- **2026-09-03** — AI kit created (AGENTS, skills, contexts, 5 specs).
- **2026-09-10** — Trigger.dev orchestration contract ratified (`research/ai-integration.md` §2a): Trigger.dev is a compute/orchestration adapter, never a data owner; the .NET backend is the only task trigger and the only writer of source-of-truth data; web/mobile never call Trigger.dev (exception: read-only Copilot realtime stream).
- **2026-09-10 (2)** — Tracker created during the cross-system audit (this system previously had none, violating the contract-sync + git-branch-flow requirement of one tracker per system). Phase P2 position + the web10↔ai02 cycle resolution recorded above.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
