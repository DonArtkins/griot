# Progress Tracker — Web

## Current State

Week 3 kit written (10 feature specs). No implementation until the backend (features 04–06) is live and the Week-1 Figma screens are finalized.

| Spec | Title | Status |
|---|---|---|
| 01 | React app setup with Vite | Pending |
| 02 | Material UI v6 integration | Pending |
| 03 | REST integration (Axios + TanStack Query) | Pending |
| 04 | GraphQL integration (Apollo Client) | Pending |
| 05 | Secure auth & state management | Pending |
| 06 | Public shell (landing/pricing/login) | Pending |
| 07 | App shell (dashboard/board/task/team/notifications) | Pending |
| 08 | Kanban board interactions (drag-drop) | Pending |
| 09 | Notifications & activity feed UI | Pending |
| 10 | Copilot panel integration | Pending |

## Session Notes

- **2026-09-03** — Web kit created (AGENTS, skills, contexts, 10 specs).
- **2026-09-07** — Design system landed: `src/theme.ts` implemented from the inspo-synthesized master tokens (`docs/design/MASTER-DESIGN-SYSTEM.md` → root `ui-tokens.md`); design-system.md + MUI skill 0.2.0 + spec 02 synced (light canvas, chrome-ink CTA, severity maps incl. Todo/Backlog). Theme is implementation-ready ahead of spec 01 scaffold; zero hardcoded colors outside the token module is now enforced by `theme.griot`.
