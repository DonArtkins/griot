# Week 02 · Diagram 09 — API Surface Map (detailed REST + GraphQL)

**Master spec + Figma Make paste prompts.** This is the "everything we just discussed" API surface — every route, every GraphQL type, every module, drawn so Postman testing + backend spec 04/05 build from the same contract.

---

## 1. REST surface (module → routes)

**Auth** `AuthController`
- `POST /api/auth/register` · `POST /api/auth/login` · `POST /api/auth/refresh` · `POST /api/auth/logout`

**Workspaces** `WorkspaceController`
- `GET/POST /api/workspaces` · `GET/PUT/DELETE /api/workspaces/{id}` · `GET/POST /api/workspaces/{id}/members` · `POST /api/workspaces/{id}/invites` · `POST /api/invites/{token}/accept`

**Projects** `ProjectController`
- `GET/POST /api/workspaces/{id}/projects` · `GET/PUT /api/projects/{id}` · `DELETE /api/projects/{id}`

**Boards / Columns** `BoardController`
- `GET/POST /api/projects/{id}/boards` · `GET /api/boards/{id}` · `POST /api/boards/{id}/columns` · `PATCH /api/columns/{id}` · `DELETE /api/columns/{id}`

**Tasks** `TaskController`
- `GET/POST /api/boards/{id}/tasks` · `GET/PUT/DELETE /api/tasks/{id}` · `PATCH /api/tasks/{id}/move` (column+position) · `PATCH /api/tasks/bulk-status` (TVP proc) · `POST /api/tasks/{id}/comments` · `GET /api/tasks/{id}/comments` · `POST /api/tasks/{id}/attachments` · `GET /api/tasks/{id}/attachments`

**Notifications** `NotificationController`
- `GET /api/notifications` · `POST /api/notifications/read-all` · `GET /api/notifications/unread-count`

**Observability**
- `GET /api/dashboard/summary?workspaceId=` (proc) · `GET /api/workspaces/{id}/activity` · `GET /api/logs/errors` (Owner/Admin) · `GET /api/logs/audit?entityType=&entityId=` (Owner)

**AI / webhooks**
- `POST /api/webhooks/trigger` (HMAC verified) · (service-token GraphQL for ai/mcp)

## 2. GraphQL surface (`/graphql`)

- **Queries**: `me`, `workspace(id)`, `projects`, `board(id)` (columns + task cards), `tasks(filter, sort, pagination)`, `task(id)`, `comments(taskId)`, `notifications`, `unreadNotificationCount`, `activityFeed(workspaceId)`, `dashboardSummary(workspaceId)`
- **Mutations**: `register`, `login`, `refresh`, `logout`, `createWorkspace`, `createProject`, `createBoard`, `createColumn`, `createTask`, `updateTask`, `moveTask`, `bulkUpdateTaskStatus`, `addComment`, `addAttachment`, `markNotificationsRead`, `acceptInvite`
- **Views/types**: `User`, `Workspace`, `Project`, `Board`, `Column`, `TaskItem`, `Comment`, `Attachment`, `ActivityLog`, `Notification`, `Invite`, `AuthPayload`, `DashboardSummary`
- **DataLoader**: batch `assignee` + `comments`
- **Filters/sorts**: on `tasks` (status, priority, assignee, dueDate) + `notifications` (readAt)

## 3. Figma Make prompts

### PROMPT A

```
API map diagram for Griot. Draw as 5 module groups (rounded dashed containers): AUTH (register/login/refresh/logout), WORKSPACES (+members+invites/accept), PROJECTS, BOARDS/COLUMNS (incl move + bulk-status), TASKS (+comments+attachments), NOTIFICATIONS (+unread), OBSERVABILITY (dashboard summary, activity, error logs, audit logs), WEBHOOKS (trigger HMAC). Inside each group list its REST routes as small pill shapes (method color: GET green, POST blue, PUT amber, PATCH purple, DELETE red). Keep method colors consistent.
```

### PROMPT B

```
To the right of the Griot API map, add a GraphQL column: box labeled "/graphql (HotChocolate)": list queries (me, workspace, projects, board, tasks, task, comments, notifications, unreadCount, activityFeed, dashboardSummary), mutations (register/login/refresh/logout, createWorkspace/Project/Board/Column/Task, updateTask, moveTask, bulkUpdateTaskStatus, addComment, addAttachment, markNotificationsRead, acceptInvite). Add a DataLoader badge 'batch assignee+comments (N+1)'. Arrow from both REST groups and GraphQL column down into a wide box 'Griot.Application services (shared, drift-proof)'. Below that: repositories (EF + Dapper procs) -> SQL Server.
```

### Fix snippets

- "Change that PATCH pill to purple (PATCH)."
- "Add GET /api/logs/errors under Observability with Owner/Admin badge."
- "Rename GraphQL box to '/graphql'."

---

## Definition of Done

- [ ] Every REST route in `api-surface.md` drawn once with method-color + module group
- [ ] GraphQL queries/mutations/types present; shared-service arrow explicit
- [ ] DataLoader + proc calls + webhook marked
- [ ] Approved → PNG → `project-kit/diagrams/architecture/api-surface-map.png`