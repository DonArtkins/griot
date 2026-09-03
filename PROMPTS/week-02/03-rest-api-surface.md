# Week 02 · Prompt 02 — REST + GraphQL API Surface (from the approved ERD)

**Tool:** Any agent (Cline/Claude). **When:** After the ERD is approved and Feature 03's schema exists.
**Research:** `research/week-02-backend-api-development.md` §4–5.

## Context

The API surface must be derived from the **approved ERD** (`project-kit/diagrams/erd/`), never invented independently. REST and GraphQL share the same service layer; the GraphQL types mirror the entities; REST routes read `resource/action`.

## Prompt (paste into the agent)

> Build **Feature 04** (`project-kit/feature-specs/04-backend-api-rest-graphql-and-auth.md`) from the approved ERD at `project-kit/diagrams/erd/`. Before writing code, verify every route and GraphQL type below against the ERD's entity/field names and update the spec if the ERD differs from this list.
>
> **REST endpoints** (all under `/api`, all JWT-protected except `auth`):
> - `POST /api/auth/register` · `POST /api/auth/login` · `POST /api/auth/refresh` · `POST /api/auth/logout`
> - `GET|POST /api/workspaces` · `GET|PUT /api/workspaces/{id}`
> - `GET|POST /api/workspaces/{id}/members` · `POST /api/workspaces/{id}/invites` · `POST /api/invites/{token}/accept`
> - `GET|POST /api/workspaces/{id}/projects` · `GET|PUT /api/projects/{id}`
> - `GET|POST /api/projects/{id}/boards` · `GET /api/boards/{id}`
> - `POST /api/boards/{id}/columns` · `PATCH /api/columns/{id}`
> - `GET|POST /api/boards/{id}/tasks` · `GET|PUT|DELETE /api/tasks/{id}`
> - `PATCH /api/tasks/bulk-status` (uses `usp_BulkUpdateTaskStatus` via Dapper + TVP)
> - `GET|POST /api/tasks/{id}/comments` · `POST /api/tasks/{id}/attachments`
> - `GET /api/workspaces/{id}/activity` · `GET /api/notifications` · `POST /api/notifications/read-all`
> - `GET /api/dashboard/summary?workspaceId=` (uses `usp_GetDashboardSummary`)
> - `POST /api/webhooks/trigger` (HMAC-verified)
>
> **GraphQL surface** (same entities, code-first): queries `me, workspace(id), projects, board(id)`, `tasks(filter, sort)`, `notifications`, `dashboardSummary`; mutations mirroring the REST mutations; `DataLoader` for `task.assignee` + `task.comments`.
>
> Follow the spec's Separation of Concerns (thin controllers/resolvers → services → repos), the auth contract (Argon2, 15-min JWT, rotated refresh, Redis rate limits, `GRIOT_SERVICE_TOKEN` → `ai-agent`), and the Docker/Deploy section (compose `api` service + health check).

## Done

- Postman collection (REST + GraphQL folders) saved for the Newman contract suite.
- Every endpoint traceable to an ERD relationship/field.