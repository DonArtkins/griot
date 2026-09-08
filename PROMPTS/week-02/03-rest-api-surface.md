# Week 02 · Prompt 03 — REST + GraphQL API Surface — EXTENSIVE MASTER

**Tool:** Any agent (Cline/Claude) + Postman. **When:** After the ERD is approved and the backend schema exists.
**Research:** `research/week-02-backend-api-development.md` §4–5 + `backend/project-kit/context/api-surface.md`.

> **No character limit.** This is the single authoritative API-surface prompt. It enumerates every route, contract, error, timing budget, and test expectation so implementation + Postman + docs all agree. It is also the input for the API-surface **diagram** in `13-diagram-api-surface.md`.

---

## 1. Master contract (from the approved ERD — never invented independently)

- Every route and GraphQL type names entities/fields **exactly** as in `diagrams/erd/`.
- REST + GraphQL share the **same service layer** (`Griot.Application`); controllers/resolvers are thin.
- Auth: JWT bearer; `GRIOT_SERVICE_TOKEN` → `ai-agent` principal for AI/MCP; HMAC webhooks.
- Errors: **404 for not-found/not-owned** (never disclose existence), 400 validation, 401 auth, 403 role, 409 conflict/state, 429 rate limit.
- Hot-path latency budgets (p95): board read < 500 ms, dashboard < 500 ms, login < 300 ms, bulk-status < 800 ms.

## 2. REST endpoints (all `/api`, JWT-protected except auth)

### Auth
- `POST /api/auth/register` — 201 `{ user, accessToken }` + `Set-Cookie: refreshToken=…; HttpOnly; Secure; SameSite=Strict` (web) · 400 invalid · 409 email exists
- `POST /api/auth/login` — 200 `{ accessToken, user }` + `Set-Cookie: refreshToken=…; HttpOnly; Secure; SameSite=Strict` (web) · 401 bad creds · 429 login throttled
  > **Refresh-token transport** (aligned with `SECURITY.md`):
  > - **Web**: refresh token delivered via `Set-Cookie: refreshToken=…; HttpOnly; Secure; SameSite=Strict` only — never in the JSON body. Web clients send it back via Cookie header; no `localStorage` access.
  > - **Mobile**: refresh token returned in JSON response body `{ refreshToken }` and stored in platform secure storage (Android Keystore / iOS Secure Enclave via Flutter `flutter_secure_storage`).
  > - **Postman / API testing**: refresh token returned in JSON response body `{ refreshToken }` and chained into the `{{refreshToken}}` environment variable for subsequent calls.
  > The `POST /api/auth/refresh` endpoint accepts either the `Cookie: refreshToken=…` header (web) or `{ "refreshToken": "…" }` JSON body (mobile/Postman).
- `POST /api/auth/refresh` — 200 rotated tokens (same transport split as above) · 401 replay/invalid (revokes FamilyId)
- `POST /api/auth/logout` — 204 (revoke refresh; clears `Set-Cookie` on web)

### Workspaces
- `GET /api/workspaces` — list (membership) · `POST /api/workspaces` — 201
- `GET/PUT/DELETE /api/workspaces/{id}` — role-gated (Owner for delete)
- `GET/POST /api/workspaces/{id}/members` · `PATCH/DELETE /api/workspaces/{id}/members/{userId}`
- `POST /api/workspaces/{id}/invites` (role + email) · `POST /api/invites/{token}/accept`

### Projects / Boards / Columns
- `GET/POST /api/workspaces/{id}/projects` · `GET/PUT/DELETE /api/projects/{id}`
- `GET/POST /api/projects/{id}/boards` · `GET /api/boards/{id}` (columns + task cards)
- `POST /api/boards/{id}/columns` · `PATCH/DELETE /api/columns/{id}`

### Tasks (core)
- `GET/POST /api/boards/{id}/tasks` (list w/ filters: status/priority/assignee; pagination)
- `GET/PUT/DELETE /api/tasks/{id}`
- `PATCH /api/tasks/{id}/move` — `{ columnId, position }` (re-order; optimistic-safe)
- `PATCH /api/tasks/bulk-status` — `{ workspaceId, taskIds[], status }` via `usp_BulkUpdateTaskStatus` TVP (atomic; 409 on any invalid id)
- `GET/POST /api/tasks/{id}/comments` · `GET/POST /api/tasks/{id}/attachments` (metadata; blob in v2)

### Notifications / activity / dashboard / observability
- `GET /api/notifications` · `POST /api/notifications/read-all` · `GET /api/notifications/unread-count`
- `GET /api/workspaces/{id}/activity` (feed, paginated)
- `GET /api/dashboard/summary?workspaceId=` — via `usp_GetDashboardSummary` (one round-trip)
- `GET /api/logs/errors` (Owner/Admin) · `GET /api/logs/audit?entityType=&entityId=` (Owner)

### AI / webhooks
- `POST /api/webhooks/trigger` — HMAC-verified (`X-Trigger-Signature`); relay for Trigger.dev background work

## 3. GraphQL surface (`/graphql`)

- **Queries**: `me`, `workspace(id)`, `projects`, `board(id)`, `tasks(filter, sort, pagination)`, `task(id)`, `comments(taskId)`, `notifications`, `unreadNotificationCount`, `activityFeed(workspaceId)`, `dashboardSummary(workspaceId)`
- **Mutations**: `register`, `login`, `refresh`, `logout`, `createWorkspace`, `createProject`, `createBoard`, `createColumn`, `createTask`, `updateTask`, `moveTask`, `bulkUpdateTaskStatus`, `addComment`, `addAttachment`, `markNotificationsRead`, `acceptInvite`
- **DataLoader**: batch `assignee` + `comments` (N+1 prevention)
- **Filters/sorts**: tasks (status, priority, assignee, dueDate), notifications (readAt)
- **Guard**: query-cost + depth limit + timeouts; same JWT principal
---

## 4. THE PROMPT — paste into the agent (no length limit)

```text
Build the backend API exactly per `backend/project-kit/context/api-surface.md` and this list. Verify every route/type against the approved ERD at `diagrams/erd/`; update the spec if the ERD differs. Enforce:
- Separation of concerns: thin controllers/resolvers to `Griot.Application` services to repos (EF 95% + Dapper for `usp_BulkUpdateTaskStatus` + `usp_GetDashboardSummary` only). Zero business logic in controllers.
- Auth: Argon2, 15-min JWT (claims sub/email/jti), rotated opaque refresh (SHA-256 at rest, FamilyId for scoped family-revoke on replay), Redis sliding-window rate limit on login + query-cost guard on /graphql, CORS allow-list. Refresh-token transport: web via `Set-Cookie: HttpOnly; Secure; SameSite=Strict` (never in JSON body); mobile via JSON body + secure storage; Postman via JSON body. The `/api/auth/refresh` endpoint accepts Cookie (web) or JSON body (mobile/Postman). Single-transaction rotation: `WHERE RevokedAt IS NULL` is the sole gate; any miss is a replay → revoke `WHERE FamilyId = @familyId`.
- Service-token principal: GRIOT_SERVICE_TOKEN to restricted ai-agent (no deletes/invites). HMAC on /api/webhooks/trigger.
- Errors: 404 on ownership miss (never disclose existence), 400 validation, 401 unauthenticated, 403 forbidden, 409 conflict/illegal state (incl. illegal task transition + bulk atomic rollback), 429 rate limit.
  - **`PATCH /api/tasks/bulk-status` specific rule:** the entire batch runs inside `usp_BulkUpdateTaskStatus` as a single TVP transaction. If **any** `taskId` in the batch is invalid (not found, wrong workspace, or wrong state), the proc rolls back the entire transaction and the endpoint returns **409** (not 404 or 400). Do not return 404 for individual missing task ids in a batch, and do not return 400 for a state mismatch — both are 409 to keep the atomic contract unambiguous. Pre-validation (empty array, malformed input) is 400 before the proc is even called.
- Pagination: fixed page size, stable order (e.g. tasks by (ColumnId, Position)); cursor or skip/take.
- Latency budgets (p95): board < 500ms, dashboard < 500ms, login < 300ms, bulk-status < 800ms — meet with indexes + DataLoader + the two procs.
- Postman: save Postman/Griot.postman_collection.json (REST + GraphQL folders, env-chained baseUrl/accessToken/refreshToken) covering every route + negative cases (401/403/404/409/429) + JSON-schema assertions on auth/task/board/dashboard.
- For every API change: update api-surface.md, the collection, docs/api, and the API-surface diagram in the SAME branch (contract-sync gate).
```

---

## 5. Refine / verify

- Re-verify the task transition endpoint rejects an illegal move with 409.
- Confirm the bulk-status proc is atomic; test a batch with one bad id → must return **409** (not 404 or 400), full rollback, zero rows updated.
- Confirm pre-validation (empty array, malformed body) returns 400 before the proc is invoked.
- Confirm the Postman collection runs end-to-end and is Newman-able (qa).

## 6. Done

- [ ] Every route/type listed above is implemented and testable via Postman
- [ ] REST + GraphQL share the same service layer (no drift)
- [ ] Error codes + pagination + latency budgets met
- [ ] Postman collection = the Newman contract suite source
- [ ] api-surface.md + collection + docs/api + diagram all synchronized

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

The current REST transport uses JSON refresh tokens for Postman/mobile. Web
HttpOnly cookie transport in the design remains a backend prerequisite for web
Feature 05; do not treat the cookie diagrams as live behavior or store tokens in
localStorage. SQL Server owns refresh rows; Redis currently owns login limits.
