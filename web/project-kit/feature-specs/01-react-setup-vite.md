# Feature 01 — React App Setup with Vite

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"React app setup with Vite"**: a TypeScript Vite app (`web/`) with its own package/lockfile, `.nvmrc`→20, dev proxy to the backend, and the Jest+RTL test harness baked in.

## Dependencies

- Backend features 04–06 live (for proxy target; app can scaffold before).

## Context To Read First

- `web/AGENTS.md` + `web/project-kit/context/{architecture,code-standards}.md`
- `research/week-03-frontend-development.md` §2

## Agent Skills To Use

- `web/.agents/skills/vite-react-setup/SKILL.md`

## Files Owned

- `web/**` (scaffold)

## Setup / Initialization

```bash
cd <repo-root>
npm create vite@latest web -- --template react-ts
cd web
npm i @mui/material @emotion/react @emotion/styled @mui/icons-material \
        @apollo/client @tanstack/react-query axios zustand react-router-dom
npm i -D @testing-library/react @testing-library/user-event @testing-library/jest-dom jest jest-environment-jsdom
```

## Implementation Notes

- Delete boilerplate; add `src/lib`, `src/features` skeletons, `src/routes` stubs.
- `vite.config.ts`: dev proxy `/api` + `/graphql` → `http://localhost:<api-port>`.
- Scripts: `lint`, `typecheck`, `test`, `build`, `preview`.

## Separation of Concerns

- Presentation only. No DB, no backend code inside `web/`. API access through `lib/` clients only.

## Docker & Deploy

- Dev: `npm run dev` (no container). Prod: Vercel Vite preset (infra spec 01).

## Out of Scope

Theme (feature 02), data wiring (03/04), auth (05), shells (06/07).

## Acceptance Criteria

- [ ] `npm run build` passes; dev server proxies `/api` + `/graphql`
- [ ] Jest test harness configured (`npm test` runs a sample test green)

## Multi-Tenant Update (2026-09-11 — PLANNED)

- Org-aware clients: when the multi-tenant wave ships, the `src/lib/` clients (Axios + Apollo) send the JWT v2 access token whose `org` claim identifies the active company (Organization) — the client never sends a tenant header or body field; scoping is server-side (backend 29).
- Type updates: new shared DTOs typed as string-union-backed interfaces — `OrganizationDto`, `OrganizationMemberDto`, `ClientProjectViewDto`, `ClientFeedbackDto`, `ProjectHandoffDto`, `HandoffDocumentDto`, `OrganizationLifecycleEventDto` — land next to the features they serve per `api-integration.md` conventions (no `any` across the API boundary).
- `OrganizationPlan` (`Free`/`Pro`/`Enterprise`) typed as display/billing metadata only — no payments this wave.
- Scaffold mechanics unchanged: Vite dev proxy, scripts, Jest harness and `.nvmrc` are untouched by the wave; multi-tenancy is a data-layer concern (specs 03–05) and new surfaces (specs 13–16).
- Canonical contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`. All items PLANNED (2026-09-11 multi-tenant migration wave) — they become acceptance evidence only when their owning spec ships on its own branch.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
