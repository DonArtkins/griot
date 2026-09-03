# Feature 05 — Secure Authentication & State Management

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"Secure authentication & state management"**: the full auth flow (register/login/logout/silent refresh) against the backend, access token in memory (Zustand), refresh token httpOnly cookie, and `RequireAuth` route guards; Zustand holds only client state.

## Dependencies

- Backend feature 08 (auth endpoints).
- Web features 03 (REST hooks) + 04 (Apollo guard).

## Context To Read First

- `web/project-kit/context/{state-and-data,api-integration}.md`

## Agent Skills To Use

- `web/.agents/skills/auth-and-zustand/SKILL.md`

## Files Owned

- `web/src/stores/authStore.ts`
- `web/src/lib/auth.ts` (register/login/logout/refreshSession)
- `web/src/routes/RequireAuth.tsx`, `public.tsx`, `protected.tsx`

## Implementation Notes

- Access token in memory only; refresh in `httpOnly; Secure; SameSite=Lax` cookie (set by the backend).
- Silent refresh on boot + 401-once-retry. Refresh failure → clear + `/login`.
- Zustand: `accessToken`, `user`, plus UI state. NEVER server entities.

## Separation of Concerns

- Auth transport in `lib/`; session state in `stores/`; guard in routes. Components call `useAuthStore`.

## Docker & Deploy

- No change; cookie flags require HTTPS in prod (Vercel provides it).

## Out of Scope

SSO, MFA (v2).

## Acceptance Criteria

- [ ] Auth flow end-to-end vs the backend; silent refresh works; session cleared on refresh failure
- [ ] No `localStorage` tokens anywhere
- [ ] `RequireAuth` redirects unauthenticated users
