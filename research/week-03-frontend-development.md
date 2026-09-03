# Week 3 — Frontend Development (React 18 + Vite 5 + MUI v6, Apollo, Axios, TanStack)

> Guide's official Week 3 goals: React app setup with Vite → Material UI v6 integration → REST integration (Axios + TanStack Query) → GraphQL integration (Apollo Client) → secure authentication & state management.
> Stack: **exact.** React 18.3, Vite 5, Material UI v6, Apollo Client, Axios, TanStack Query 5 — nothing else. `[own-stack]`: Zustand (client state), React Router (routing), GSAP/Lenis (Public-shell motion only).

---

## 1. Decisions & Rationale

1. **Bootcamp scaffold, driver by the guide:** `npm create vite@latest griot-web -- --template react-ts`, then `npm i @mui/material @emotion/react @emotion/styled @mui/icons-material @apollo/client @tanstack/react-query axios zustand react-router-dom`. No Next.js — the guide says Vite, and Vercel (Week 5) intentionally auto-detects the **Vite framework preset**.
2. **MUI v6 with a custom skin** — the Week-1 Figma tokens are imported into `createTheme` (colors, typography, shape) so the app doesn't look like default-purple MUI. The design system work in Week 1 now pays off here as the theme file.
3. **Data fetching split (unchanged from Guide lines):**
   - **Apollo Client** → GraphQL reads (dashboard, boards, task detail) from the Week-2 HotChocolate endpoint.
   - **Axios + TanStack Query 5** → REST (auth, fast mutations, file upload), full cache config in one `QueryClient`.
   - `[own-stack]` **Zustand** → only client-only state (filters, modal open/close, drag state). Server data never duplicates into Zustand.
4. **Auth flow** — the Week-2 JWT pairing: access token in memory (Zustand store), refresh token in an `HttpOnly` cookie, silent-refresh on boot. No `localStorage` tokens (XSS surface), which Week 6's OWASP review explicitly checks.

## 2. SPA structure (two surfaces, one app)

```
web/
├── index.html
├── vite.config.ts          # proxy /api + /graphql to localhost:PORT in dev
└── src/
    ├── main.tsx            # StrictMode + Providers (Apollo, QueryClient, Theme)
    ├── theme.ts            # MUI theme ← Week-1 tokens
    ├── lib/
    │   ├── apolloClient.ts
    │   ├── apiClient.ts    # Axios instance + 401→refresh→retry interceptor
    │   └── queryClient.ts
    ├── stores/authStore.ts # Zustand
    ├── routes/
    │   ├── public.tsx      # /, /pricing, /login, /signup — GSAP/Lenis via dynamic import
    │   └── protected.tsx   # /app/* + RequireAuth wrapper
    └── features/…          # dashboard, board, taskDetail, settings, notifications
```

- **Public routes** are lazy-imported; GSAP/Lenis only touches this subtree.
- **App routes** sit behind `RequireAuth` → redirect to `/login` when no access token.

## 3. Data fetching wiring (guide-exact trio)

```ts
// src/lib/apolloClient.ts
import { ApolloClient, InMemoryCache, createHttpLink } from "@apollo/client";
import { useAuthStore } from "@/stores/authStore";

export const apolloClient = new ApolloClient({
  link: new HttpLink({
    uri: import.meta.env.VITE_API_URL + "/graphql",
    headers: () => ({ authorization: `Bearer ${useAuthStore.getState().accessToken ?? ""}` }),
  }),
  cache: new InMemoryCache({ typePolicies: { Task: { fields: { order: { merge: false } } } } }),
});
```

```ts
// src/lib/apiClient.ts — Axios + TanStack
export const api = axios.create({ baseURL: import.meta.env.VITE_API_URL });
api.interceptors.request.use(cfg => { /* attach Bearer from store */ });
api.interceptors.response.use(r => r, async err => {
  if (err.response?.status === 401 && !err.config._retried) {
    err.config._retried = true;
    const ok = await refreshSession();      // POST /auth/refresh (httpOnly cookie)
    return ok ? api(err.config) : Promise.reject(err);
  }
  return Promise.reject(err);
});
```
TanStack Query v5 config: `new QueryClient({ defaultOptions: { queries: { staleTime: 30_000 } } })`; feature hooks (`useBoardQuery` etc.) select from the Apollo cache or query cache — **never** mirrored into Zustand.

## 4. Auth (secure, as specified)

| Piece | Where | Why (Week 6 OWASP line) |
|---|---|---|
| Access token | Zustand store (memory) | no XSS-readable storage |
| Refresh token | `httpOnly; Secure; SameSite=Lax` cookie | JS cannot read it; validated server-side in Week 2 |
| Silent refresh | on app boot + 401 interceptor | seamless sessions; verified in Week 6 tests |

### 4.5 Griot Copilot panel (own-stack AI — see `ai-integration.md`)

A collapsible chat panel on the right of the board/task views. User prompt → Trigger.dev agent run → tool calls against the Week-2 GraphQL → **streaming** back via Trigger realtime (websocket) rendered into a MUI chat thread.

- **Propose-before-write**: mutations come back as a tappable card ("Create task *…* in In Progress?") — the user approves, then the app calls the .NET API itself (not the agent) so the write is a plain, testable mutation.
- Provider wiring (illustrative — verify against the current `@trigger.dev/react-hooks` exports):
```tsx
import { RealtimeProvider, useRealtimeTask, useTrigger } from "@trigger.dev/react-hooks";

<RealtimeProvider accessToken={triggerAccessToken}>
  <CopilotPanel />
</RealtimeProvider>
```
- In CI, the panel is tested with a **stubbed copilot** (MSW intercept) — never a real LLM.

## 5. Risks & Watch-outs
- MUI re-theme must be complete before pages are built (default-purple risk)
- Apollo cache merge bugs on board reorder — handled via `typePolicies`
- GSAP only in public subtree; React StrictMode double-invoke → use `useGSAP()`
- Lighthouse budget on the deployed shell recorded in Week 6/7
- Copilot: LLM latency must never block board interactivity → only render on stream, never await before paint

## 6. Definition of Done — Week 3
- [ ] Vite + React 18 + TS app, MUI theme from Week-1 tokens
- [ ] Apollo Client wired; REST Axios + TanStack wired; Zustand only client-state
- [ ] Public shell (GSAP/Lenis) + App shell behind auth guard
- [ ] Board view: drag-drop, Apollo optimistic updates, REST persistence
- [ ] Auth flow end-to-end vs. the .NET backend
- [ ] Copilot panel streams an answer + proposes a mutation; approval flow creates a task via REST (AI layer)
- [ ] `npm run build` passes; Lighthouse run recorded

---
**Engineering Excellence. Production Mindset. Professional Impact.**