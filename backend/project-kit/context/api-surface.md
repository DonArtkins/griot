# Backend API Surface (REST + GraphQL)

This file is the **cross-system API contract**. Web, mobile, AI, MCP, and the Postman collection all derive from it. Changes trigger the contract-sync gate.

## REST (`/api/*`) — controllers are thin wrappers over services

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/auth/register` · `/login` · `/refresh` · `/logout` | auth lifecycle |
| GET/POST | `/api/workspaces` | list/create |
| GET/PUT | `/api/workspaces/{id}` | read/update |
| GET/POST | `/api/workspaces/{id}/members` | member list/invite |
| POST | `/api/invites/{token}/accept` | accept invite |
| GET/POST | `/api/workspaces/{id}/projects` | project list/create |
| GET/PUT | `/api/projects/{id}` | read/update/archive |
| GET/POST | `/api/projects/{id}/boards` | board list/create |
| GET | `/api/boards/{id}` | board with columns+tasks |
| POST | `/api/boards/{id}/columns` · PATCH `/api/columns/{id}` | column create/update |
| GET/POST | `/api/boards/{id}/tasks` | task list/create |
| GET/PUT/DELETE | `/api/tasks/{id}` | task CRUD |
| PATCH | `/api/tasks/bulk-status` | TVP bulk status (proc) |
| GET/POST | `/api/tasks/{id}/comments` | comments |
| POST | `/api/tasks/{id}/attachments` | attachment metadata |
| GET | `/api/workspaces/{id}/activity` | activity feed |
| GET | `/api/notifications` · POST `/api/notifications/read-all` | notifications |
| GET | `/api/dashboard/summary?workspaceId=` | dashboard (proc) |
| POST | `/api/webhooks/trigger` | Trigger.dev webhook (HMAC) |

## GraphQL (`/graphql`)

- Queries: `me`, `workspace(id)`, `projects`, `board(id)`, `tasks(filter, sort)`, `notifications`, `dashboardSummary`.
- Mutations mirror the REST mutations (create/update/move task, comment, etc.).
- `DataLoader` for `Task.assignee` and `Task.comments`.
- Auth: same JWT bearer; query-cost guard middleware.

## Conventions

- REST + GraphQL call the SAME services → drift-proof.
- Responses are DTOs, never raw entities.
- Errors: 404 for not found/not-owned (never disclose existence), 400 validation, 401 auth, 403 role, 429 rate limit.
- Postman collection (`Postman/Griot.postman_collection.json`) mirrors every route; Newman reuses it in CI (qa).
