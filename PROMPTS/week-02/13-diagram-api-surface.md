# Week 02 · Diagram 09 — API Surface Map (detailed REST + GraphQL)

**Master spec + Figma Make paste prompts.** This is the "everything we just discussed" API surface — every route, every GraphQL type, every module, drawn so Postman testing + backend spec 04/05 build from the same contract.

---

## 1. REST surface (module → routes)

> **Authoritative source:** `backend/project-kit/context/api-surface.md`. This diagram must remain synchronized with that file. Any route change updates both in the same branch.

**Auth** `AuthController`
- `POST /api/auth/register` · `POST /api/auth/login` · `POST /api/auth/refresh` · `POST /api/auth/logout`

**Workspaces** `WorkspaceController`
- `GET/POST /api/workspaces` · `GET/PUT/DELETE /api/workspaces/{id}` (DELETE = Owner only)
- `GET/POST /api/workspaces/{id}/members` · `PATCH/DELETE /api/workspaces/{id}/members/{userId}`
- `POST /api/workspaces/{id}/invites` (role + email) · `POST /api/invites/{token}/accept`

**Projects** `ProjectController`
- `GET/POST /api/workspaces/{id}/projects` · `GET/PUT/DELETE /api/projects/{id}`

**Boards / Columns** `BoardController`
- `GET/POST /api/projects/{id}/boards` · `GET /api/boards/{id}` (columns + task cards)
- `POST /api/boards/{id}/columns` · `PATCH /api/columns/{id}` · `DELETE /api/columns/{id}`

**Tasks** `TaskController`
- `GET/POST /api/boards/{id}/tasks` (filters: status/priority/assignee; paginated)
- `GET/PUT/DELETE /api/tasks/{id}`
- `PATCH /api/tasks/{id}/move` — `{ columnId, position }` (re-order; optimistic-safe)
- `PATCH /api/tasks/bulk-status` — `{ workspaceId, taskIds[], status }` via `usp_BulkUpdateTaskStatus` TVP (atomic; 409 on any invalid id in batch)
- `GET/POST /api/tasks/{id}/comments`
- `GET/POST /api/tasks/{id}/attachments` (metadata; blob upload in v2)
  > **Note:** both GET and POST are valid on `/api/tasks/{id}/attachments`. GET retrieves attachment metadata list; POST creates a new attachment record. No inconsistency.

**Notifications** `NotificationController`
- `GET /api/notifications` · `POST /api/notifications/read-all` · `GET /api/notifications/unread-count`

**Observability / Dashboard** `DashboardController` / `LogController`
- `GET /api/dashboard/summary?workspaceId=` — via `usp_GetDashboardSummary` (one round-trip)
- `GET /api/workspaces/{id}/activity` (feed, paginated)
- `GET /api/logs/errors` (Owner/Admin) · `GET /api/logs/audit?entityType=&entityId=` (Owner)

**AI / webhooks** `WebhookController`
- `POST /api/webhooks/trigger` (HMAC-verified via `X-Trigger-Signature`; relay for Trigger.dev background work)
- (service-token GraphQL path for ai/mcp — see §2)

## 2. GraphQL surface (`/graphql`)

> Same contract as `backend/project-kit/context/api-surface.md`. REST + GraphQL share the same service layer — both lists must stay in sync.

- **Queries**: `me`, `workspace(id)`, `projects`, `board(id)` (columns + task cards), `tasks(filter, sort, pagination)`, `task(id)`, `comments(taskId)`, `notifications`, `unreadNotificationCount`, `activityFeed(workspaceId)`, `dashboardSummary(workspaceId)`
- **Mutations**: `register`, `login`, `refresh`, `logout`, `createWorkspace`, `updateWorkspace`, `deleteWorkspace`, `inviteMember`, `acceptInvite`, `updateMember`, `removeMember`, `createProject`, `updateProject`, `deleteProject`, `createBoard`, `createColumn`, `updateColumn`, `deleteColumn`, `createTask`, `updateTask`, `deleteTask`, `moveTask`, `bulkUpdateTaskStatus`, `addComment`, `addAttachment`, `markNotificationsRead`
- **Types**: `User`, `Workspace`, `WorkspaceMember`, `Project`, `Board`, `Column`, `TaskItem`, `Comment`, `Attachment`, `ActivityLog`, `Notification`, `Invite`, `AuthPayload`, `DashboardSummary`, `NotificationCount`
- **DataLoader**: batch `assignee` + `comments` (N+1 prevention)
- **Filters/sorts**: on `tasks` (status, priority, assignee, dueDate) + `notifications` (readAt)
- **Guards**: query-cost limit + depth limit + timeouts; same JWT principal; `GRIOT_SERVICE_TOKEN` → ai-agent scope

## 3. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make. It draws the complete API surface (REST + GraphQL + shared service layer) in one pass.

```text
API map diagram for Griot. Build it in two halves:

LEFT HALF — REST module groups (rounded dashed containers), inside each a pill shape per route, METHOD-COLORED (GET green, POST blue, PUT amber, PATCH purple, DELETE red):

- AUTH: POST /api/auth/register; POST /api/auth/login; POST /api/auth/refresh; POST /api/auth/logout
- WORKSPACES: GET/POST /api/workspaces; GET/PUT/DELETE /api/workspaces/{id} (DELETE=Owner only); GET/POST /api/workspaces/{id}/members; PATCH/DELETE /api/workspaces/{id}/members/{userId}; POST /api/workspaces/{id}/invites; POST /api/invites/{token}/accept
- PROJECTS: GET/POST /api/workspaces/{id}/projects; GET/PUT/DELETE /api/projects/{id}
- BOARDS/COLUMNS: GET/POST /api/projects/{id}/boards; GET /api/boards/{id}; POST /api/boards/{id}/columns; PATCH/DELETE /api/columns/{id}
- TASKS: GET/POST /api/boards/{id}/tasks; GET/PUT/DELETE /api/tasks/{id}; PATCH /api/tasks/{id}/move; PATCH /api/tasks/bulk-status (TVP proc; 409 on invalid batch); GET/POST /api/tasks/{id}/comments; GET/POST /api/tasks/{id}/attachments
- NOTIFICATIONS: GET /api/notifications; POST /api/notifications/read-all; GET /api/notifications/unread-count
- OBSERVABILITY: GET /api/dashboard/summary?workspaceId= (usp_GetDashboardSummary); GET /api/workspaces/{id}/activity; GET /api/logs/errors (Owner/Admin badge); GET /api/logs/audit?entityType=&entityId= (Owner badge)
- WEBHOOKS: POST /api/webhooks/trigger (HMAC X-Trigger-Signature verified)

RIGHT HALF — GraphQL column:
Box "/graphql (HotChocolate)".
Queries: me, workspace(id), projects, board(id), tasks(filter,sort,pagination), task(id), comments(taskId), notifications, unreadNotificationCount, activityFeed(workspaceId), dashboardSummary(workspaceId).
Mutations: register, login, refresh, logout, createWorkspace, updateWorkspace, deleteWorkspace, inviteMember, acceptInvite, updateMember, removeMember, createProject, updateProject, deleteProject, createBoard, createColumn, updateColumn, deleteColumn, createTask, updateTask, deleteTask, moveTask, bulkUpdateTaskStatus, addComment, addAttachment, markNotificationsRead.
Types: User, Workspace, WorkspaceMember, Project, Board, Column, TaskItem, Comment, Attachment, ActivityLog, Notification, Invite, AuthPayload, DashboardSummary, NotificationCount.
DataLoader badge: "batch assignee + comments (N+1 prevention)".
Filters/sorts on tasks (status, priority, assignee, dueDate) + notifications (readAt).
Guards badge: "query-cost + depth limit + timeouts; GRIOT_SERVICE_TOKEN → ai-agent scope".

BELOW BOTH HALVES:
- A wide box "Griot.Application — shared service layer (REST + GraphQL both delegate here — drift-proof)".
- Arrows: every REST group → shared layer; GraphQL box → shared layer.
- Below that: repositories (EF Core 95% + Dapper procs usp_BulkUpdateTaskStatus / usp_GetDashboardSummary) → SQL Server.

ANNOTATION: "Postman collection mirrors this map 1:1 (backend feature 07); a route change updates api-surface.md + the collection + this diagram together."
STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```

### Refine

- "Change that PATCH pill to purple (PATCH)."
- "Add GET /api/logs/errors under Observability with Owner/Admin badge."
- "Rename GraphQL box to '/graphql'."

---

## Definition of Done

- [ ] Every REST route in `backend/project-kit/context/api-surface.md` drawn once with method-color + module group (including workspace DELETE, member PATCH/DELETE, GET /api/tasks/{id}/attachments, unread-count, log endpoints, task move)
- [ ] GraphQL queries/mutations/types all present (including `task(id)`, `comments(taskId)`, `unreadNotificationCount`, `activityFeed`, complete mutations list, all types); shared-service arrow explicit
- [ ] DataLoader + proc calls (usp_BulkUpdateTaskStatus, usp_GetDashboardSummary) + webhook (HMAC) marked
- [ ] `GET/POST /api/tasks/{id}/attachments` shown as two separate pills — no inconsistency with §1 or `api-surface.md`
- [ ] Approved → PNG → `diagrams/architecture/api-surface-map.png`