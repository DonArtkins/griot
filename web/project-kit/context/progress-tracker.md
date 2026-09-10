# Progress Tracker — Web

## Current State

**Phase P1** in `docs/planning/IMPLEMENTATION-ROADMAP.md` — the next layer after backend P0 closes (backend 09 → 20 → 18 → 19 → 22 → 21 → 11 → 10). Kit written (10 specs). **Not started.** 9 of 10 specs depend only on backend specs that are all ✅ (04–08, 13–17, 07 auth). Spec 10 is deliberately held out of P1: it needs ai 01–02 (see P2 in the roadmap).

| Spec | Title | Status | Blocked by |
|---|---|---|---|
| 01 | React app setup with Vite | Pending (P1 entry point) | backend 04–06 ✅ (proxy target) |
| 02 | Material UI v6 integration | Pending | w 01; theme already in `src/theme.ts` |
| 03 | REST integration (Axios + TanStack Query) | Pending | w 01, backend 04 ✅ + 07 ✅ |
| 04 | GraphQL integration (Apollo) | Pending | w 03, backend 05 ✅ |
| 05 | Secure auth & state management | Pending | w 03+04, backend 07 ✅ |
| 06 | Public shell (landing/pricing/login) | Pending | w 01–02, 05 |
| 07 | App shell (dashboard/board/task/team/notifications) | Pending | w 03–05, backend 04–07 ✅ |
| 08 | Kanban board interactions (drag-drop) | Pending | w 07, backend 06 ✅ |
| 09 | Notifications & activity feed UI | Pending | w 07, backend 04–06 ✅ |
| 10 | Copilot panel integration | Pending (**P2**) | w 05+07, **ai 01–02** |

## Roadmap Order (canonical, from IMPLEMENTATION-ROADMAP.md P1–P2)

`web 01 → 02 → 03 → 04 → 05 → 06 → 07 → 08 → 09` → *(AI hop: ai 01 → ai 02)* → `web 10`

**Why web is the next layer after backend:** it is the bootcamp's next graded week (Week 3), the primary demo surface, and every dependency is already green. **The web10↔ai02 cycle is resolved in the roadmap:** ai 02's spec references web 10 as its *consumer* (contract-design dependency, not build-order) — build ai 01–02 first, then web 10, then ai 04's propose-before-write wraps the panel's approval cards.

## Session Notes

- **2026-09-10 (2)** — Layer-order audit (docs-only): tracker rebuilt — stale "no implementation until backend 04–06 live" removed (those are ✅); each spec now carries its blocker; web 10 explicitly deferred to roadmap phase P2 behind ai 01–02; canonical order `01→…→09`, AI hop, then `10` per `docs/planning/IMPLEMENTATION-ROADMAP.md`.
- **2026-09-03** — Web kit created (AGENTS, skills, contexts, 10 specs).
- **2026-09-07** — Design system landed: `src/theme.ts` implemented from the inspo-synthesized master tokens (`docs/design/MASTER-DESIGN-SYSTEM.md` → root `ui-tokens.md`); design-system.md + MUI skill 0.2.0 + spec 02 synced (light canvas, chrome-ink CTA, severity maps incl. Todo/Backlog). Theme is implementation-ready ahead of spec 01 scaffold; zero hardcoded colors outside the token module is now enforced by `theme.griot`.


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
