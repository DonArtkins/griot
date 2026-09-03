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
