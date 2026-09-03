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

## 3. The prompt (single, extensive — no length limit)

Paste the full prompt below into Figma Make. It draws the layers AND the arrows in one extensive pass.

```text
C4 Component diagram (Level 3) showing the INSIDE of the Griot 'api' container only. Build the layers top-to-bottom:

TOP LAYER — thin controllers (equal-width paper-thin boxes):
REST: AuthController, WorkspaceController, ProjectController, BoardController, TaskController, CommentController, NotificationController, WebhookController
MIDDLE-LEFT — GraphQL (a vertical column):
GriotQuery, GriotMutation, DataLoader (batch assignees + comments)
CENTER — THE SERVICE LAYER (a wide, dominant block):
"Griot.Application — the service layer" containing exactly: AuthService, WorkspaceService, ProjectService, BoardService, TaskService, CommentService, NotificationService
BOTTOM LAYER — repositories + infrastructure:
EF Core repositories (95% CRUD) | Dapper repositories (usp_BulkUpdateTaskStatus, usp_GetDashboardSummary) | GriotDbContext | Redis client | Auth middleware (JWT validation → principal; GRIOT_SERVICE_TOKEN → ai-agent principal)

ARROWS (this is the important part — draw exactly these):
- REST controllers → the service layer (one arrow each; label "thin shell, no logic")
- GriotQuery/GriotMutation → the service layer (same color as REST arrows = same layer)
- DataLoader → repositories (label "batch assignees/comments — N+1 prevention")
- service layer → repository interface block (label "dependency inversion")
- repositories → GriotDbContext (label "EF"), repositories → Sql/procs (label "Dapper")
- Auth middleware → controllers + GraphQL (label "JWT principal / service token→ai-agent")
- Redis client ↔ service layer (label "rate limit, refresh, budgets"); Redis client → external Redis (dashed)
- GriotDbContext → external SQL Server (dashed); Dapper repos → external SQL Server (dashed)

ANNOTATION (a callout box): "ONE service layer, TWO API surfaces (REST + GraphQL), ZERO business logic in controllers/resolvers — drift-proof by construction."
```

### Refine

- "Draw all controllers as equal-width thin boxes above the service layer."
- "Make the GraphQL→services arrows the same color as REST→services."
- "Label the DataLoader edges 'batch assignees/comments (N+1 prevent)'."

---

## Definition of Done

- [ ] All components present; controllers/resolvers visually "thin"; services block dominates
- [ ] REST + GraphQL both arrow into the same services block
- [ ] DataLoader, EF repos, Dapper procs, Redis client, auth middleware drawn
- [ ] Approved → PNG → `project-kit/diagrams/architecture/api-component.png`