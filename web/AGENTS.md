# AGENTS.md — Griot Web Frontend (React 18 + Vite 5 + MUI v6)

## Read This First

You are the agent for the **Web** system of Griot (bootcamp Week 3). You build presentation only — the web app never reaches a database; all data flows through the backend's REST + GraphQL API (`backend/`).

Stack (exact): React 18.3, Vite 5, Material UI v6, Apollo Client, Axios, TanStack Query 5. [own-stack]: Zustand (client state), React Router, GSAP + Lenis (Public shell only).

## Two shells (Week-1 Figma design system)

- **Public shell** — landing, pricing, login/signup. Spectacle + conversion; GSAP/Lenis via dynamic import; Lighthouse budget; axe-clean.
- **App shell** — dashboard, board, task detail, team settings, notifications. Speed + clarity; MUI transitions only; status colors = the ERD enums.

## Folder shape

```
web/
├── index.html
├── vite.config.ts            # /api + /graphql dev proxy
└── src/
    ├── main.tsx              # StrictMode + Providers (Apollo, QueryClient, Theme, Router)
    ├── theme.ts              # MUI theme <- ui-tokens
    ├── lib/{apolloClient,apiClient,queryClient}.ts
    ├── stores/authStore.ts   # Zustand: accessToken + client-only state
    ├── routes/{public,protected}.tsx
    └── features/{dashboard,board,taskDetail,settings,notifications,copilot}/…
```

## Reading Order

1. Root `AGENTS.md` + root context (`integration-contracts.md` for the API surface).
2. `research/week-03-frontend-development.md`.
3. `web/project-kit/context/{design-system,state-and-data,api-integration,code-standards}.md`.
4. The current spec (one at a time, numeric order).

## Required Skills

Root shared skills (`contract-sync`, `git-branch-flow`, `throttling-prevention`) + `web/.agents/skills/` (`vite-react-setup`, `material-ui-theme`, `apollo-graphql`, `tanstack-rest`, `auth-and-zustand`). Follow the relevant `SKILL.md` exactly.

## Where This System Sits in the Build Order (canonical: `docs/planning/IMPLEMENTATION-ROADMAP.md`)

**Phase P1 — the next layer after backend P0 closes (backend 09 → 20 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10).** Own order: **01 → 02 → 03 → 04 → 05 → 06 → 07 → 08 → 09**, then the P2 AI hop (ai 01 → ai 02) unblocks **spec 10 (Copilot panel)**; **spec 11 (AI Reports & Audit Center — award-grade UX, `inspo/`) starts after ai 06/07 + backend 24; spec 12 (dedicated AI Workspace sidebar — charts, threads, report composition, SuperAdmin ops console) starts after ai 09 + backend 26**. Do not start 10 before ai 02 exists. Entry branch: `feature/web/01-react-setup-vite`. Track state in `web/project-kit/context/progress-tracker.md`.

## Verification Gates

- `npm run lint && npm run typecheck && npm test && npm run build` all green.
- No server data in Zustand; no `localStorage` tokens.
- Empty/loading/error states on every async surface; interaction tests for TaskCard/BoardView/modals.
- Contracts (route names, theme tokens, GraphQL fragments) synchronized.

## Hard Rules

1. App shell never imports GSAP; Public shell is the only motion-heavy surface.
2. Board drag-drop is web-only; mobile uses a picker.
3. Copilot mutations: propose → user approval → app performs the write (agent never writes directly).
4. Theme colors come only from tokens; status chip colors match the enums.

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**

## Implemented authentication contract (Feature 07)

Use the [auth contract](../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

Before committing or pushing implementation, run `python3 scripts/check-contract-sync.py` from
the repository root. Synchronize the owning spec, dependent specs, planning,
research, docs, contexts, agent instructions, diagram sources and progress notes
in the feature branch. Planned behavior must be labeled and must not count as
implemented acceptance evidence. Run the system verification gates as well.

## Audit synchronization — 2026-09-11

Current implementation remains backend 09 review hardening; next is backend 20 after review. Future planning is not completed implementation. P0: backend 09 → 20 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10. P2: ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12. Full requirement/review ledger: `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
