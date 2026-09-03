---
name: tanstack-rest
description: "Axios + TanStack Query 5 for REST reads/mutations: one QueryClient, interceptors (401->refresh->retry), feature hooks."
metadata:
  version: "0.1.0"
---

# TanStack Query + Axios Skill

## QueryClient

```ts
// src/lib/queryClient.ts
export const queryClient = new QueryClient({ defaultOptions: { queries: { staleTime: 30_000 } } });
```

## Axios instance

- `src/lib/apiClient.ts`: request interceptor attaches Bearer from the auth store; response interceptor on 401 calls the refresh endpoint once, then retries the original request once.

## Feature hooks

- `useBoardQuery`, `useMoveTask`, `useBulkStatus`, `useInvite` live in their feature folder.
- Mutations invalidate/refetch the touched queries; toasts on success/error.

## Rules

- REST handles: auth, mutations, uploads, bulk ops. GraphQL handles reads (Apollo).
- No duplicate server state in Zustand.
