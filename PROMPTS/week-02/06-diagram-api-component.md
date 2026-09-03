# Week 02 · Diagram 03 — C4 Component Diagram (API only, Level 3)

**Master spec + Figma Make paste prompts.** Opens the `api` container: the module split, the shared service layer, the two API surfaces (REST + GraphQL) both delegating to it, the DataLoader, the Redis client, and the repositories (EF + Dapper). This is where "one service layer, two API surfaces" is *drawn*, not described.

---

## 1. Components inside the `api` container

| Component | Owns | Depends on |
|---|---|---|
| **AuthController** | register/login/refresh/logout (`/api/auth/*`) | AuthService |
| **WorkspaceController** | `/api/workspaces`, members, invites | WorkspaceService |
| **ProjectController** | `/api/projects` | ProjectService |
| **BoardController** | `/api/boards`, columns | BoardService |
| **TaskController** | `/api/tasks`, comments, attachments, bulk-status | TaskService, CommentService |
| **NotificationController** | `/api/notifications` | NotificationService |
| **WebhookController** | `/api/webhooks/trigger` (HMAC) | (job relay) |
| **GriotQuery (GraphQL)** | `me`, `workspace`, `projects`, `board(id)`, `tasks`, `notifications`, `dashboardSummary` | Services + DataLoader |
| **GriotMutation (GraphQL)** | mirrors REST mutations | Services |
| **DataLoader** | batch loads assignees + comments | Repositories |
| **Griot.Application (services)** | AuthService, WorkspaceService, ProjectService, BoardService, TaskService, CommentService, NotificationService — ALL business rules | Repos (interfaces) |
| **Repositories** | EF Core repos (95%) + Dapper repos (2 procs) | DbContext / SqlConnection |
| **GriotDbContext** | EF Core 8 mappings, migrations | SQL Server |
| **Redis client** | rate limit, refresh metadata, token budgets | Redis |
| **Auth middleware** | JWT validation → principal (`sub`, `wid`); `GRIOT_SERVICE_TOKEN` → ai-agent principal | — |

## 2. Reading rules (draw these as annotations)

- Controllers + GraphQL resolvers are **thin shells**; arrows point DOWN into services only. No arrow from one controller to another (no business logic in controllers).
- REST and GraphQL both arrow into the SAME `Griot.Application` services block (drift-proof).
- Services arrow into repository interfaces; repositories arrow into DbContext (EF) and Sql/ (Dapper procs).
- DataLoader sits between resolvers and repositories (N+1 prevention).
- Redis + SQL Server are dashed-edge externals at the bottom.

## 3. Figma Make prompts (≤2000 chars each)

### PROMPT A — the layers + blocks

```
C4 Component diagram (L3) showing the INSIDE of the Griot 'api' container only. Top layer three columns of thin boxes: REST controllers (AuthController, WorkspaceController, ProjectController, BoardController, TaskController, CommentController, NotificationController, WebhookController) — one paper-thin shell each. Middle-left: GraphQL column (GriotQuery, GriotMutation, DataLoader). Center-wide block labeled "Griot.Application — the service layer" containing 7 services (Auth, Workspace, Project, Board, Task, Comment, Notification). Bottom layer: repositories (EF Core repos 95% + Dapper repos: usp_BulkUpdateTaskStatus, usp_GetDashboardSummary), GriotDbContext, Redis client, Auth middleware. No business logic anywhere except the service layer block.
```

### PROMPT B — the arrows (this is the important part)

```
Arrow rules for the Griot api L3 diagram:
controllers -> services (all 7, one arrow per controller)
GraphQL resolvers (GriotQuery/GriotMutation) -> services
DataLoader -> repositories (assignees, comments)
services -> repository interface block
repositories -> GriotDbContext (EF) and -> Sql/procs (Dapper)
Auth middleware -> controllers (JWT principal, service token->ai-agent)
Redis client <-> services (rate limit, refresh, budgets) and -> Redis external
DbContext -> SQL Server external (dashed); repos Sql/ -> SQL Server external (dashed)
Label each arrow short (thin shell, no logic; shared layer; batch N+1; proc).
The message this diagram must show: ONE service layer, TWO API surfaces, zero logic in controllers.
```

### Fix snippets

- "Draw all controllers as equal-width thin boxes above the service layer."
- "Make the GraphQL->services arrows the same color as REST->services (same layer)."
- "Label the DataLoader edges 'batch assignees/comments (N+1 prevent)'."

---

## Definition of Done

- [ ] All components present; controllers/resolvers visually "thin"; services block dominates
- [ ] REST + GraphQL both arrow into the same services block
- [ ] DataLoader, EF repos, Dapper procs, Redis client, auth middleware drawn
- [ ] Approved → PNG → `project-kit/diagrams/architecture/api-component.png`