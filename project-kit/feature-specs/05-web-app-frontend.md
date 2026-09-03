# Feature 05 — Web App Frontend (React 18 + Vite 5 + MUI v6)

## Type

NEW FEATURE

## What This Delivers

The web application with the two shells: Public shell (landing, pricing, login/signup with GSAP/Lenis) and App shell (dashboard, board, task detail, team settings, notifications). Data via Apollo (GraphQL) + Axios/TanStack Query (REST), client state via Zustand, auth against the Feature-04 backend with silent refresh.

## Dependencies

- Feature 04 (backend REST + GraphQL + auth live locally).
- Week-1 design system tokens (ui-context/ui-tokens/ui-rules/ui-registry).
- Node 20 via `.nvmrc`.

## Context To Read First

- `context/ui-context.md`, `ui-tokens.md`, `ui-rules.md`, `ui-registry.md`
- `context/library-docs.md` (web section)
- `research/week-03-frontend-development.md`

## Files Owned

- `web/**` (own package, own `.nvmrc`, own lockfile)

## Files

CREATE: `web/` Vite React-TS app; `src/theme.ts` (MUI from tokens), `src/lib/{apolloClient,apiClient,queryClient}.ts`, `src/stores/authStore.ts` (Zustand).
CREATE: `web/src/routes/public.tsx` + `protected.tsx` (RequireAuth) via React Router.
CREATE: `web/src/features/{dashboard,board,taskDetail,settings,notifications}/` — pages + hooks + types + components from `ui-registry.md`.
CREATE: Copilot panel surface (feature-flagged; real streaming in Feature 07).
CREATE: `web/src/test/setupTests.ts` + Jest config; tests for `cn()`, theme tokens, interactive components.
MODIFY: `vite.config.ts` — dev proxy `/api` + `/graphql` → backend; build config for Vercel (Vite preset).

## Setup / Initialization

```bash
cd <repo-root>
npm create vite@latest web -- --template react-ts
cd web && nvm use
npm i @mui/material @emotion/react @emotion/styled @mui/icons-material \
        @apollo/client @tanstack/react-query axios zustand react-router-dom
npm i -D @testing-library/react @testing-library/user-event @testing-library/jest-dom jest jest-environment-jsdom
echo "VITE_API_URL=http://localhost:PORT" > .env.local   # dev points at the .NET API
npm run dev
```

## Separation of Concerns

- **Data**: Apollo caches GraphQL server data; TanStack Query caches REST data. Zustand holds client-only state (filters, modal open/close, drag state, in-memory access token). Never duplicate server data into Zustand.
- **Surfaces**: Public routes are a separate lazy-loaded subtree (`GSAP`/`Lenis` only there); App routes behind `RequireAuth`.
- **Features**: each feature folder owns its pages, hooks, types, and GraphQL fragments — no shared "everything" components folder beyond the registry.
- **Auth**: access token in memory (Zustand); refresh token in `httpOnly; Secure; SameSite=Lax` cookie; silent-refresh on boot + 401-once-retry interceptor.

## Docker & Deploy

- **Dev**: Vite dev server proxying to the compose-started API; no container needed.
- **Prod**: Vercel with the **Vite framework preset** — push `web/` to GitHub → import → preset Vite → set `VITE_API_URL` → deploy. Assets served from `dist` (built by Vercel's build step).
- A `web/Dockerfile` (nginx static) is **optional** for Railway-only hosts; Vercel is the canonical path per the bootcamp.
- Lighthouse run captured after deploy (LCP < 2.5 s, CLS < 0.1).

## Out of Scope

- Real copilot streaming (Feature 07), SSR/Next.js, mobile-specific components.

## Acceptance Criteria

- [ ] MUI theme from tokens — no default-purple, no hardcoded colors
- [ ] Public shell + protected App shell render; auth flow end-to-end vs backend
- [ ] Board drag-drop with optimistic cache + REST persistence works
- [ ] `npm run lint && npm run typecheck && npm test && npm run build` green
- [ ] Deployed on Vercel (Vite preset) talking to the deployed API; Lighthouse recorded

## Future Modifications

- Feature 07 wires the Copilot panel to Trigger realtime; Feature 10 adds Cypress + axe.