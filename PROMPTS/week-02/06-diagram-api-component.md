# Week 02 · Diagram 03 — C4 Component Diagram (API only, Level 3)

**Master spec + Figma Make paste prompts.** Opens the `api` container: the module split, the shared service layer, the two API surfaces (REST + GraphQL) both delegating to it, the DataLoader, the Redis client, and the repositories (EF + Dapper). This is where "one service layer, two API surfaces" is *drawn*, not described.

---

## 1. Components inside the `api` container

> **Controller topology decision (binding):** comments and attachments are sub-resources of tasks. They are handled by dedicated thin controllers (`CommentController`, `AttachmentController`) that own those routes. `TaskController` owns only task-level routes. This keeps each controller focused and matches the route structure. Do not assign comments/attachments to `TaskController` in this diagram or in code.

| Component | Owns | Depends on |
|---|---|---|
| **AuthController** | `POST /api/auth/*` (register/login/refresh/logout) | AuthService |
| **WorkspaceController** | `/api/workspaces`, `/api/workspaces/{id}/members`, `/api/workspaces/{id}/invites`, `/api/invites/{token}/accept` | WorkspaceService |
| **ProjectController** | `/api/workspaces/{id}/projects`, `/api/projects/{id}` | ProjectService |
| **BoardController** | `/api/projects/{id}/boards`, `/api/boards/{id}`, `/api/boards/{id}/columns`, `/api/columns/{id}` | BoardService |
| **TaskController** | `/api/boards/{id}/tasks`, `/api/tasks/{id}`, `/api/tasks/{id}/move`, `/api/tasks/bulk-status` | TaskService |
| **CommentController** | `/api/tasks/{id}/comments` | CommentService |
| **AttachmentController** | `/api/tasks/{id}/attachments` | AttachmentService |
| **NotificationController** | `/api/notifications`, `/api/notifications/read-all`, `/api/notifications/unread-count` | NotificationService |
| **DashboardController** | `/api/dashboard/summary`, `/api/workspaces/{id}/activity`, `/api/logs/errors`, `/api/logs/audit` | DashboardService |
| **WebhookController** | `POST /api/webhooks/trigger` (HMAC) | WebhookRelayService |
| **GriotQuery (GraphQL)** | `me`, `workspace`, `projects`, `board(id)`, `tasks`, `task(id)`, `comments(taskId)`, `notifications`, `unreadNotificationCount`, `activityFeed`, `dashboardSummary` | Services + DataLoader |
| **GriotMutation (GraphQL)** | mirrors REST mutations | Services |
| **DataLoader** | batch loads assignees + comments | Repositories |
| **Griot.Application (services)** | AuthService · WorkspaceService · ProjectService · BoardService · TaskService · CommentService · **AttachmentService** · NotificationService · DashboardService · **WebhookRelayService** — ALL business rules | Repos (interfaces) |
| **Repositories** | EF Core repos (95%) + Dapper repos (2 procs: usp_BulkUpdateTaskStatus, usp_GetDashboardSummary) | DbContext / SqlConnection |
| **GriotDbContext** | EF Core 8 mappings, migrations | SQL Server |
| **Redis client** | rate limit, refresh metadata, token budgets | Redis |
| **Auth middleware** | JWT validation → principal (`sub`, `email`, `jti`); `GRIOT_SERVICE_TOKEN` → ai-agent principal | — |

> **AttachmentService** responsibility: validates attachment metadata (mime type, size limits), persists `Attachments` row via EF repo, returns signed URL or metadata DTO. Blob storage (Azure Blob / S3) is v2 — v1 stores `StorageUrl` as a placeholder.
>
> **WebhookRelayService** responsibility: verifies HMAC signature (`X-Trigger-Signature`), deserializes the Trigger.dev event payload, and dispatches the appropriate application-layer action (e.g. creating an activity log entry, triggering a notification). It is the relay interface between the external webhook caller and the application layer — no direct DB access.

## 2. Reading rules (draw these as annotations)

- Controllers + GraphQL resolvers are **thin shells**; arrows point DOWN into services only. No arrow from one controller to another (no business logic in controllers).
- REST and GraphQL both arrow into the SAME `Griot.Application` services block (drift-proof).
- Services arrow into repository interfaces; repositories arrow into DbContext (EF) and Sql/ (Dapper procs).
- DataLoader sits between resolvers and repositories (N+1 prevention).
- Redis + SQL Server are dashed-edge externals at the bottom.
- Observability contract (ERD tables, added pre-code): every request → `ApiLogs`; AI tool calls + state-changes → `ActivityLogs` + `AuditLogs` (workspaceId · tool · payloadHash · runId) — 90-day hot retention, PII-redacted.

## 3. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make. It draws the layers AND the arrows in one extensive pass.

```text
C4 Component diagram (Level 3) showing the INSIDE of the Griot 'api' container only. Build the layers top-to-bottom:

TOP LAYER — thin controllers (equal-width paper-thin boxes):
REST: AuthController, WorkspaceController, ProjectController, BoardController, TaskController, CommentController, AttachmentController, NotificationController, DashboardController, WebhookController
(Note: CommentController owns /api/tasks/{id}/comments; AttachmentController owns /api/tasks/{id}/attachments; TaskController owns task-level routes only)

MIDDLE-LEFT — GraphQL (a vertical column):
GriotQuery, GriotMutation, DataLoader (batch assignees + comments)

CENTER — THE SERVICE LAYER (a wide, dominant block):
"Griot.Application — the service layer" containing exactly:
AuthService · WorkspaceService · ProjectService · BoardService · TaskService · CommentService · AttachmentService · NotificationService · DashboardService · WebhookRelayService
(AttachmentService: validates metadata, persists Attachments row, returns StorageUrl DTO)
(WebhookRelayService: verifies HMAC X-Trigger-Signature, deserializes Trigger.dev event, dispatches to app layer — relay interface only, no direct DB)

BOTTOM LAYER — repositories + infrastructure:
EF Core repositories (95% CRUD) | Dapper repositories (usp_BulkUpdateTaskStatus, usp_GetDashboardSummary) | GriotDbContext | Redis client | Auth middleware (JWT validation → principal; GRIOT_SERVICE_TOKEN → ai-agent principal)

ARROWS (this is the important part — draw exactly these):
- REST controllers → the service layer (one arrow each; label "thin shell, no logic")
- GriotQuery/GriotMutation → the service layer (same color as REST arrows = same layer)
- DataLoader → repositories (label "batch assignees/comments — N+1 prevention")
- service layer → repository interface block (label "dependency inversion")
- repositories → GriotDbContext (label "EF"), repositories → Dapper procs (label "Dapper: usp_BulkUpdateTaskStatus, usp_GetDashboardSummary")
- Auth middleware → controllers + GraphQL (label "JWT principal / service token→ai-agent")
- Redis client ↔ service layer (label "rate limit, refresh, budgets"); Redis client → external Redis (dashed)
- GriotDbContext → external SQL Server (dashed); Dapper repos → external SQL Server (dashed)
- WebhookRelayService → ActivityLogService / NotificationService (label "dispatches event")

ANNOTATION (a callout box): "ONE service layer, TWO API surfaces (REST + GraphQL), ZERO business logic in controllers/resolvers — drift-proof by construction. CommentController + AttachmentController are separate thin wrappers (not folded into TaskController). Observability: ApiLogs on every request; ActivityLogs + AuditLogs on AI + state changes (payloadHash ↔ runId), 90-day retention."
STYLE: light canvas (#F7F8FA), white boxes with 1px hairlines, token-named fills only (per docs/design/MASTER-DESIGN-SYSTEM.md), readable at 100% zoom, one page.
```

### Refine

- "Draw all controllers as equal-width thin boxes above the service layer."
- "Make the GraphQL→services arrows the same color as REST→services."
- "Label the DataLoader edges 'batch assignees/comments (N+1 prevent)'."

---

## Definition of Done

- [ ] All components present; controllers/resolvers visually "thin"; services block dominates
- [ ] **Controller topology enforced:** CommentController owns `/api/tasks/{id}/comments`; AttachmentController owns `/api/tasks/{id}/attachments`; TaskController owns task-level routes only — no ambiguity
- [ ] **AttachmentService declared** in the service layer with its responsibility note
- [ ] **WebhookRelayService declared** in the service layer (HMAC verify + event dispatch relay) — not labelled "(job relay)"
- [ ] REST + GraphQL both arrow into the same services block
- [ ] DataLoader, EF repos, Dapper procs (named), Redis client, auth middleware drawn
- [ ] DashboardController + DashboardService present (covers `/api/logs/*` + `/api/dashboard/summary`)
- [ ] Approved → PNG → `diagrams/architecture/api-component.png`