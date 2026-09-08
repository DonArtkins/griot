# State & Data (web)

## The split (absolute)

| Store | Holds | Source |
|---|---|---|
| Apollo `InMemoryCache` | GraphQL read data (dashboard, boards, task detail) | `/graphql` |
| TanStack Query cache | REST data (auth, mutations, uploads, bulk ops) | `/api` |
| Zustand `authStore` | `accessToken` in memory + client-only UI state | — |

## Apollo

- Single client; `typePolicies` like `Task.order: { merge: false }` prevent board-reorder cache bugs.
- Fragments colocate with components.

## TanStack Query

- `staleTime: 30_000`; mutations invalidate affected keys; toasts for success/error.
- `apiClient` axios interceptor: attach Bearer; 401 → `/auth/refresh` (httpOnly cookie) → retry once (`_retried` guard).

## Zustand (client-only state ONLY)

Filters, modal open/close, drag state, UI prefs, `accessToken`. Mirrored server data is a contract violation.

## Auth

Access token in memory (Zustand). Refresh token `httpOnly; Secure; SameSite=Lax` cookie. Silent refresh on boot; refresh failure → clear + redirect `/login`.

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

The current REST transport uses JSON refresh tokens for Postman/mobile. Web
HttpOnly cookie transport in the design remains a backend prerequisite for web
Feature 05; do not treat the cookie diagrams as live behavior or store tokens in
localStorage. SQL Server owns refresh rows; Redis currently owns login limits.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
