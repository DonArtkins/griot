# API Integration (web → backend)

The web app calls the backend exactly as specified in `project-kit/context/integration-contracts.md` and `backend/project-kit/context/api-surface.md`.

- **GraphQL (Apollo)**: reads — `me`, `workspace`, `projects`, `board(id)`, `tasks`, `notifications`, `dashboardSummary`.
- **REST (Axios + TanStack)**: auth, uploads, bulk-status, invites, webhook-independent mutations.
- **Base URL**: `import.meta.env.VITE_API_URL` (dev proxy `/api` + `/graphql`; prod = deployed API).
- **Errors**: 401 → refresh+retry once; 403 → role UI; 429 → backoff message; 404/409 handled per-surface.
- **Typed clients**: shared types live next to the feature they serve; no `any` across the API boundary.

When the backend changes a route/type, update this file + the feature hooks in the same branch (contract-sync).
