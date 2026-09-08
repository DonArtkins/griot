# Backend API Surface (REST + GraphQL)

This file is the **cross-system API contract**. Web, mobile, AI, MCP, and the Postman collection all derive from it. Changes trigger the contract-sync gate (update this file + all dependent specs + docs/api + the API-surface diagram in the same branch).

## REST (`/api/*`) — controllers are thin wrappers over services

| Method | Route | Purpose | Role gate |
|---|---|---|---|
| POST | `/api/auth/register` · `/login` · `/refresh` · `/logout` | auth lifecycle | public / authenticated |
| GET/POST | `/api/workspaces` | list/create | authenticated |
| GET/PUT/DELETE | `/api/workspaces/{id}` | read/update/delete | DELETE = Owner only |
| GET/POST | `/api/workspaces/{id}/members` | member list/add | Owner/Admin |
| PATCH/DELETE | `/api/workspaces/{id}/members/{userId}` | update/remove member | Owner (remove), Admin (update role) |
| POST | `/api/workspaces/{id}/invites` | invite by email + role | Owner/Admin |
| POST | `/api/invites/{token}/accept` | accept invite | authenticated |
| GET/POST | `/api/workspaces/{id}/projects` | project list/create | workspace member |
| GET/PUT/DELETE | `/api/projects/{id}` | read/update/archive/delete | DELETE = Owner/Admin |
| GET/POST | `/api/projects/{id}/boards` | board list/create | workspace member |
| GET | `/api/boards/{id}` | board with columns+tasks | workspace member |
| POST | `/api/boards/{id}/columns` | column create | Owner/Admin |
| PATCH/DELETE | `/api/columns/{id}` | column update/delete | Owner/Admin |
| GET/POST | `/api/boards/{id}/tasks` | task list (filters: status/priority/assignee; paginated) / create | workspace member |
| GET/PUT/DELETE | `/api/tasks/{id}` | task CRUD | DELETE = Owner/Admin |
| PATCH | `/api/tasks/{id}/move` | move task to column+position (optimistic-safe) | workspace member |
| PATCH | `/api/tasks/bulk-status` | TVP bulk status via `usp_BulkUpdateTaskStatus` (atomic; 409 on any invalid id in batch) | Owner/Admin/Member (workspace) |
| GET/POST | `/api/tasks/{id}/comments` | list/create comments | workspace member |
| GET/POST | `/api/tasks/{id}/attachments` | list attachment metadata / upload to Vercel Blob (Phase 1) | workspace member |
| DELETE | `/api/tasks/{id}/attachments/{attachmentId}` | delete attachment (blob + DB metadata) | workspace member |
| GET | `/api/workspaces/{id}/activity` | activity feed (paginated) | workspace member |
| GET/POST | `/api/notifications` · POST `/api/notifications/read-all` | list / mark all read | authenticated |
| GET | `/api/notifications/unread-count` | unread notification count | authenticated |
| GET | `/api/dashboard/summary?workspaceId=` | dashboard via `usp_GetDashboardSummary` (one round-trip; Phase 1: Redis 60s cache) | workspace member |
| GET | `/api/logs/errors` | error log (Owner/Admin) | Owner/Admin |
| GET | `/api/logs/audit?entityType=&entityId=` | audit log | Owner |
| POST | `/api/webhooks/trigger` | Trigger.dev webhook (HMAC `X-Trigger-Signature`) | HMAC only |
| GET | `/health` | liveness (health checks) | public |

## Attachment limits (Phase 1 — Vercel Blob)
- **File size:** 25 MB per file (enforced via `[RequestSizeLimit(26_214_400)]`)
- **Workspace quota:** 100 MB total per workspace (checked before upload)
- **MIME types:** Whitelist (images, PDF, .docx, .xlsx); blacklist executables (.exe, .dll, .bat, .sh, .ps1)
- **Storage:** Vercel Blob free tier (1 GB + 10 GB transfer/month). Public URLs served via Vercel CDN (512 MB cache limit per blob).
- **Access control model:** Bearer-by-URL (Vercel Blob generates signed URLs with embedded tokens). Workspace membership checked at upload/delete; URL access relies on URL secrecy. For strict private access in Phase 3, migrate to R2 with authenticated download endpoints.
- **Migration path (Phase 3):** Cloudflare R2 when egress >100 GB/month or when private access enforcement required. See `feature-specs/11-blob-storage-integration.md`.

## Pagination (Phase 1 — abuse prevention)
- **MaxPageSize:** 1,000 items (enforced via middleware)
- **Default page size:** 100 items (if client doesn't specify)
- **Applies to:** Tasks list, activity feed, notifications, audit logs
- **Response:** 400 error if client requests >1,000 items with message: "Page size cannot exceed 1,000 items"

## GraphQL (`/graphql`)

- **Queries**: `me`, `workspace(id)`, `projects(workspaceId, filter, sort)`, `board(id)`, `tasks(boardId, filter, sort, pagination)` — **`boardId` is required** (an unscoped task query would cross workspace boundaries), `task(id)`, `comments(taskId)`, `notifications`, `unreadNotificationCount`, `activityFeed(workspaceId)`, `dashboardSummary(workspaceId)`
- **Mutations** (spec 05 — only fully implemented fields are registered; auth spec 07, bulk/column/invite spec 06, attachments spec 11 register theirs later): `createWorkspace`, `updateWorkspace`, `deleteWorkspace`, `createProject`, `updateProject`, `deleteProject`, `createBoard`, `createTask`, `updateTask`, `deleteTask`, `addComment`
- **Types**: `User`, `Workspace`, `WorkspaceMember`, `Project`, `Board`, `Column`, `TaskItem`, `Comment`, `Attachment`, `ActivityLog`, `Notification`, `Invite`, `AuthPayload`, `DashboardSummary`, `NotificationCount`
- **DataLoader (Phase 1 — N+1 prevention)**: `AssigneeDataLoader` batches `Task.assignee` queries, `CommentDataLoader` batches `Task.comments` queries. **Impact:** Board with 50 tasks: 101 queries → 3 queries (1 board + 1 batch assignees + 1 batch comments). See `feature-specs/05-graphql-layer-hotchocolate.md`.
- **Response caching (Phase 2)**: Board queries cached in Redis by `(boardId, workspaceId, userId, timestamp)` key. Invalidated on writes. Configured via `.AddQueryCachePipeline().AddRedisQueryStorage()`. **Impact:** Hot boards served from Redis (~1ms) instead of SQL (~50–200ms).
- **Filters/sorts**: tasks (status, priority, assignee, dueDate), notifications (readAt). **Pagination**: MaxPageSize = 1,000.
- **Authorization**: `[Authorize]` authenticates only; every workspace-scoped resolver and mutation additionally enforces caller ownership/membership per resource (`Workspaces.OwnerId` or a `WorkspaceMembers` row) before reading/mutating. Cross-workspace resource IDs are rejected. Destructive ops (delete workspace/project/task) require Owner/Admin. No global filter — enforcement is per-resolver.
- **Guards**: parser caps 256 fields / 512 nodes + max execution depth 10 (introspection excluded) + 30 s execution timeout + global rate limiter; same JWT bearer; `GRIOT_SERVICE_TOKEN` → ai-agent scope (no deletes/invites).

## Controller topology

| Controller | Routes | Service(s) |
|---|---|---|
| AuthController | `/api/auth/*` | AuthService |
| WorkspaceController | `/api/workspaces*`, `/api/invites/*` | WorkspaceService |
| ProjectController | `/api/workspaces/{id}/projects`, `/api/projects/{id}` | ProjectService |
| BoardController | `/api/projects/{id}/boards`, `/api/boards/{id}`, `/api/boards/{id}/columns`, `/api/columns/{id}` | BoardService |
| TaskController | `/api/boards/{id}/tasks`, `/api/tasks/{id}`, `/api/tasks/{id}/move`, `/api/tasks/bulk-status` | TaskService |
| CommentController | `/api/tasks/{id}/comments` | CommentService |
| AttachmentController | `/api/tasks/{id}/attachments` | AttachmentService |
| NotificationController | `/api/notifications*` | NotificationService |
| DashboardController | `/api/dashboard/summary`, `/api/workspaces/{id}/activity`, `/api/logs/*` | DashboardService |
| WebhookController | `/api/webhooks/trigger` | WebhookRelayService |

## Conventions

- REST + GraphQL call the SAME services (`Griot.Application`) → drift-proof.
- Responses are DTOs, never raw entities.
- Errors: 404 for not found/not-owned (never disclose existence), 400 validation, 401 auth, 403 role, 409 conflict/illegal state, 429 rate limit.
- Every response carries `X-Request-Id` (request-id middleware; logs correlated by it).
- **`PATCH /api/tasks/bulk-status`**: 409 for any invalid task id in batch (not 404/400); full rollback via `usp_BulkUpdateTaskStatus`; 400 only for pre-validation failures (empty array, malformed body).
- Pagination: fixed page size, stable order (tasks by `(ColumnId, Position)`); cursor or skip/take.
- Latency budgets (p95): board read < 500 ms, dashboard < 500 ms, login < 300 ms, bulk-status < 800 ms.
- Postman collection (`Postman/Griot.postman_collection.json`) mirrors every route; Newman reuses it in CI (qa).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
