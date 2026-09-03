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
cd web && nvm use
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
