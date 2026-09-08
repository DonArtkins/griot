# API Documentation — Griot

> The Postman-first API docs. **The Postman collection is the source of truth** (backend feature 08); this folder renders it for humans and links to the contract.

## Collections & contract

- **Source of truth**: `Postman/Griot.postman_collection.json` (REST + GraphQL folders, env-chained).
- **Rendered API reference** (choose at implementation):
  - Option A — **Stoplight / Postman published docs**: publish the collection → live docs URL.
  - Option B — **Redoc / Scalar** static page generated from the OpenAPI export of the REST controllers (via NSwag/Swashbuckle) + a GraphQL schema page (`/graphql?sdl`).
- The full route/type surface is in `backend/project-kit/context/api-surface.md`, and the big **API surface diagram** is at `PROMPTS/week-02/13-diagram-api-surface.md` (→ `diagrams/architecture/api-surface-map.png`).

## What Postman will test (backend feature 08 scope)

- Auth (register/login/refresh/logout; chained token env vars).
- Workspaces → members → invites → accept.
- Projects → boards → columns → tasks (CRUD, move, bulk-status).
- Comments, attachments, notifications, dashboard summary, activity.
- GraphQL folder: same entities via queries/mutations with schema assertions.
- Negative cases: 400/401/403/404/409/429 + JSON schema assertions on key contracts.

## Docs to keep in sync

1. `backend/project-kit/context/api-surface.md` — the machine-readable route/type contract.
2. `Postman/Griot.postman_collection.json` — executable contract (Newman in CI).
3. This folder — human-facing reference.
**Rule**: a route change updates all three + dependent web/mobile/ai/mcp specs in the same branch (contract-sync).

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**