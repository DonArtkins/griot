# Week 02 · Prompt 01 — Database Schema Design in Figma Make (ERD)

> 📌 **This is today's deliverable — the first item of Week 2 in the GTP 2026 roadmap: "Database Schema Design in Figma."**
> **Tool:** Figma Make (the AI prototyping surface), live project:
> `https://www.figma.com/make/bTB7eE6s39O6yvtYfq0DeR/Griot?p=f&t=CrPqLvvqtlfVbwSx-0`
> **Output lives at:** `project-kit/diagrams/erd/` (export a PNG frame into that folder — Feature 02 and every schema decision reads it).
> **Research input:** `research/week-01-fundamentals-and-system-design.md` §4.4 (screen → entity table) + `research/week-02-backend-api-development.md` §3.

---

## 1. What we are building

Griot is a project-management web app (workspaces → projects → boards → columns → tasks) with comments, attachments, roles/invites, an activity feed, and notifications. The Week-1 Figma screens defined the entities; **this ERD is the translation of those screens into a relational schema for SQL Server 2022**, which the EF Core data layer (backend feature 02) will then transcribe 1:1.

**Guarding rule:** every entity below traces to a Week-1 screen. If it's not on a screen, it's not in this ERD.

## 2. The entity roster (16 tables + 4 enums)

Derived from the Week-1 entity/screen mapping + the Lyncxs observability/error-tracking conventions. Use **exactly** these names, types, and relationships. These names ARE the contract for `Griot.Domain` entities, GraphQL types, TS/Dart models, and MCP tool schemas. Adding `ApiLogs`/`ErrorLogs`/`AuditLogs` now (before code) means we never come back to redesign the schema later.

### Enums (draw as a legend panel, v1 values fixed)
| Enum | Values |
|---|---|
| `TaskStatus` | Backlog · Todo · InProgress · InReview · Done |
| `Priority` | Low · Medium · High · Urgent |
| `WorkspaceRole` | Owner · Admin · Member |
| `NotificationType` | Mention · Assignment · DueDate · System |
| `ErrorFixStatus` | Open · Investigating · Fixed · Verified · WonTFix (for ErrorLogs) |

### Tables
**Users** — `Id` (GUID, PK) · `Email` (nvarchar(320), UQ) · `DisplayName` (nvarchar(100)) · `AvatarUrl` (nullable) · `PasswordHash` (nvarchar(512), Argon2) · `CreatedAt` (datetime2, SYSUTCDATETIME) · `UpdatedAt`

**Workspaces** — `Id` (GUID, PK) · `Name` (nvarchar(100)) · `Slug` (nvarchar(100), UQ) · `OwnerId` (FK → Users) · `CreatedAt` · `UpdatedAt`

**WorkspaceMembers** (join Users ↔ Workspaces) — `WorkspaceId` (FK, PK part) · `UserId` (FK, PK part) · `Role` (enum WorkspaceRole) · `JoinedAt` (datetime2)

**Invites** — `Id` (GUID, PK) · `WorkspaceId` (FK) · `Email` (nvarchar(320)) · `Role` (enum) · `Token` (nvarchar(128), UQ — single-use) · `Status` (Pending · Accepted · Declined · Expired) · `InvitedById` (FK → Users) · `ExpiresAt` · `CreatedAt`

**Projects** — `Id` (GUID, PK) · `WorkspaceId` (FK) · `Name` (nvarchar(150)) · `Key` (nvarchar(10), short code) · `Description` (nvarchar(max), nullable) · `Status` (Active · Archived) · `CreatedAt` · `UpdatedAt`

**Boards** — `Id` (GUID, PK) · `ProjectId` (FK) · `Name` (nvarchar(100)) · `Order` (int) · `CreatedAt`

**Columns** (explicit table, not hardcoded statuses) — `Id` (GUID, PK) · `BoardId` (FK) · `Name` (nvarchar(100)) · `Order` (int) · `WipLimit` (int, nullable) · `CreatedAt`

**TaskItems** — `Id` (GUID, PK) · `BoardId` (FK) · `ColumnId` (FK → Columns) · `Title` (nvarchar(200)) · `Description` (nvarchar(max), nullable) · `Status` (enum TaskStatus) · `Priority` (enum Priority) · `AssigneeId` (FK → Users, nullable) · `CreatorId` (FK → Users) · `DueDate` (date, nullable) · `Position` (decimal — order within the column) · `CreatedAt` · `UpdatedAt`

**Comments** — `Id` (GUID, PK) · `TaskId` (FK → TaskItems) · `AuthorId` (FK → Users) · `Body` (nvarchar(max)) · `CreatedAt` · `UpdatedAt`

**Attachments** — `Id` (GUID, PK) · `TaskId` (FK) · `UploaderId` (FK → Users) · `FileName` (nvarchar(255)) · `MimeType` (nvarchar(100)) · `SizeBytes` (bigint) · `StorageUrl` (nvarchar(500)) · `CreatedAt`

**ActivityLogs** (the feed + the AI audit trail) — `Id` (GUID, PK) · `WorkspaceId` (FK) · `ActorId` (FK → Users) · `EntityType` (nvarchar(50)) · `EntityId` (GUID) · `Action` (nvarchar(50)) · `Payload` (nvarchar(max) JSON, nullable) · `CreatedAt`

**Notifications** — `Id` (GUID, PK) · `UserId` (FK → Users) · `Type` (enum NotificationType) · `Title` (nvarchar(200)) · `Body` (nvarchar(500), nullable) · `TargetRef` (nvarchar(200)) · `ReadAt` (datetime2, nullable) · `CreatedAt`

**RefreshTokens** — `Id` (GUID, PK) · `UserId` (FK → Users) · `TokenHash` (nvarchar(128), UQ — SHA-256 of the opaque token) · `ExpiresAt` · `RevokedAt` (nullable) · `ReplacedByTokenId` (GUID, nullable — rotation chain) · `CreatedAt`

### Observability & audit (added in planning so the schema never changes later)

**ApiLogs** — `Id` (GUID, PK) · `RequestId` (GUID, UQ — correlation/tracing) · `UserId` (FK → Users, nullable) · `Method` (nvarchar(8)) · `Path` (nvarchar(300)) · `QueryString` (nvarchar(500), nullable) · `StatusCode` (int) · `DurationMs` (int) · `UserAgent` (nvarchar(300), nullable) · `IpAddress` (nvarchar(45), nullable — masked per privacy) · `CreatedAt`

**ErrorLogs** — `Id` (GUID, PK) · `RequestId` (GUID, nullable) · `UserId` (FK → Users, nullable) · `ExceptionType` (nvarchar(200)) · `Message` (nvarchar(max)) · `StackTrace` (nvarchar(max), nullable) · `Source` (nvarchar(100), nullable — Api/GraphQL/Agent/Worker) · `FixStatus` (enum ErrorFixStatus, default Open) · `SolvedByUserId` (FK → Users, nullable) · `FixedAt` (datetime2, nullable) · `CreatedAt`

**AuditLogs** — `Id` (GUID, PK) · `ActivityId` (FK → ActivityLogs, nullable) · `ActorId` (FK → Users) · `Action` (nvarchar(50)) · `EntityType` (nvarchar(50)) · `EntityId` (GUID) · `Before` (nvarchar(max) JSON, nullable — snapshot pre-change) · `After` (nvarchar(max) JSON, nullable — snapshot post-change) · `CreatedAt`

## 3. Relationships & cardinality (draw these edges)

| From | To | Cardinality | Notes |
|---|---|---|---|
| Users | Workspaces | M:N via `WorkspaceMembers` | Role on the association |
| Workspaces | Users (Owner) | N:1 | `Workspaces.OwnerId` |
| Workspaces | WorkspaceMembers | 1:N | cascade members |
| Workspaces | Projects | 1:N | |
| Workspaces | ActivityLogs | 1:N | feed + AI audit |
| Workspaces | Invites | 1:N | |
| Users | RefreshTokens | 1:N | rotation chain `ReplacedByTokenId` |
| Projects | Boards | 1:N | |
| Boards | Columns | 1:N | ordered |
| Columns | TaskItems | 1:N | `Position` within column |
| TaskItems | Users (Assignee) | N:1 nullable | `AssigneeId` |
| TaskItems | Users (Creator) | N:1 | `CreatorId` |
| TaskItems | Comments | 1:N | cascade |
| TaskItems | Attachments | 1:N | cascade |
| Users | Notifications | 1:N | `ReadAt` nullable = unread |
| Users | ApiLogs | 1:N | `UserId` nullable, request tracing |
| Users | ErrorLogs | 1:N | `UserId` nullable, error attribution |
| ActivityLogs | AuditLogs | 1:N | `ActivityId` optional link |

## 4. Indexes to annotate (sticky notes beside the tables)

- Every FK column → non-clustered index.
- `TaskItems(ColumnId, Position)` — board read + drag hot path.
- `TaskItems(AssigneeId)` — "my tasks"; `TaskItems(DueDate)` — reminder agent.
- `ActivityLogs(WorkspaceId, CreatedAt DESC)` — feed + `summarize_project`.
- `Notifications(UserId, ReadAt)` — unread count.
- `RefreshTokens(UserId)`, `RefreshTokens(TokenHash)` unique — rotation lookup.
- `Invites(Token)` unique; `Invites(WorkspaceId, Email)` — no duplicate pending invites.
- `ApiLogs(RequestId)` unique — request tracing; `ApiLogs(UserId, CreatedAt)`; `ApiLogs(Path)`.
- `ErrorLogs(FixStatus)` partial (Open/Investigating); `ErrorLogs(FixedAt)` — pruning.
- `AuditLogs(ActorId)`; `AuditLogs(ActivityId)`.

> Use the **master prompt file** (`02-erd-figma-make-master-prompt.md`) for building in Figma Make — one extensive prompt covering all 16 tables, 5 enums, 19 relationships, and 13 indexes. If the canvas truncates, add any missing table with a short add-on prompt (name the table + "as in my first prompt").

---

## 5. The prompt (paste into Figma Make)

Open your Figma Make project **Griot** (`https://www.figma.com/make/bTB7eE6s39O6yvtYfq0DeR/Griot`), then paste this into Make's AI prompt/describe step (use **Plan mode** to steer before generating):

> Build an **entity-relationship diagram** for **Griot**, a project-management web app. Target database: **SQL Server 2022**. The diagram is the implementation contract for an Entity Framework Core data layer, so entity names, field names, types, and relationships must match exactly. Enums are labeled panels, not tables.
>
> **Entities (13):** `Users`, `Workspaces`, `WorkspaceMembers` (M:N join with `Role`), `Invites`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `ActivityLogs`, `Notifications`, `RefreshTokens`. Use the exact field lists from the spec doc (GUID PKs, `nvarchar` types, FK labels, nullable `?` badges, unique `UQ` badges).
>
> **Enums (4):** `TaskStatus` Backlog·Todo·InProgress·InReview·Done; `Priority` Low·Medium·High·Urgent; `WorkspaceRole` Owner·Admin·Member; `NotificationType` Mention·Assignment·DueDate·System. Put these in a legend panel.
>
> **Relationships (15):** draw crow's-foot edges with FK labels — WorkspaceMembers joins Users↔Workspaces with Role on the association; Projects→Boards→Columns→TaskItems chain; TaskItems has TWO FKs to Users (`AssigneeId` nullable, `CreatorId` required); Comments/Attachments belong to TaskItems; Workspaces own Projects, ActivityLogs, Invites; Users own RefreshTokens (rotation chain via `ReplacedByTokenId`) and Notifications.
>
> **Style:** group and color-code by module — Identity/Auth (Users, RefreshTokens, WorkspaceMembers, Invites), Core Board (Workspaces, Projects, Boards, Columns, TaskItems), Social (Comments, Attachments), Observability (ActivityLogs, Notifications). Add a legend (PK / FK / UQ / nullable) and a column of sticky-note indexes: every FK indexed; `TaskItems(ColumnId, Position)`; `TaskItems(AssigneeId)`; `TaskItems(DueDate)`; `ActivityLogs(WorkspaceId, CreatedAt DESC)`; `Notifications(UserId, ReadAt)`; unique `TokenHash`, `Invites.Token`, `Users.Email`, `Workspaces.Slug`. Keep it legible at 100% zoom; no overlapping edges.

## 6. Step-by-step: building the ERD in Figma Make

### Step 1 — Open the project
Open the live project: **https://www.figma.com/make/bTB7eE6s39O6yvtYfq0DeR/Griot?p=f&t=CrPqLvvqtlfVbwSx-0** (this is your Week-2 canvas inside the same Figma account used for the Week-1 design system).

### Step 2 — Use the AI Describe step (Plan mode)
1. Click into the Make canvas and open the **AI / Describe** field.
2. Choose **Plan mode** first: paste the §5 prompt, let Make outline its plan (tables + relationships), review that the plan matches the entity roster, then **Generate**.
3. If Make's draft misses a table or a field, tell it exactly what to add in a follow-up prompt before building by hand.

### Step 3 — Fix the tables by hand (precision pass)
Make's first pass is a *rough clay model*. On the Make canvas:
1. Arrange one **shape/table per entity** (13 total). Snap-guides keep them aligned.
2. Header = dark fill, white bold **PascalCase** name (`TaskItems`, `ActivityLogs`).
3. Fields one row each: `Id · GUID · PK`, `?` for nullable, `UQ` for unique, `→` for FKs.
4. Duplicate a polished table shape (`Ctrl/Cmd+D`) then retitle — keep visual rhythm identical.

### Step 4 — Color-code modules
Fill each header per module: Identity/Auth = purple · Core Board = blue · Social = teal · Observability = amber.

### Step 5 — Draw the 15 relationship edges
Use the **connector line** tool between node handles; set orthogonal/crisp routing; put an arrow on the "many" side; label each edge `TaskItems.AssigneeId → Users.Id` etc.

### Step 6 — Add legend, enums, index notes
1. **Legend** (top-left): PK / FK / UQ / nullable symbols.
2. **Enums panel** (right): the 4 enum chips/tables.
3. **Index notes** (bottom or right column): one sticky per index from §4, dashed-connected to its table.

### Step 7 — Refine (≤2 rounds)
Read it like a reviewer: does every Week-1 screen's entity appear? (Dashboard → Workspace/Project/ActivityLog; Board → Board/Column/TaskItem; Task detail → TaskItem + Comment + Attachment; Team settings → User + WorkspaceMember + Invite; Notifications → Notification.) No orphan table. Tidy overlaps; verify the 15 edges against §3.

### Step 8 — Approve + export
1. Human approves the ERD in the Make project.
2. Export the ERD frame as **PNG** → save as `griot-erd-v1.0.0.png` into `project-kit/diagrams/erd/`.
3. Update `project-kit/diagrams/README.md` ledger (name, version, date, status=✅ approved, source=Figma Make project URL).

### Step 9 — Feed it to the code agent
When prompting the backend agent (Cline/Claude), say: *"Build backend features 01–02 from the approved ERD `project-kit/diagrams/erd/griot-erd-v1.0.0.png` (produced in Figma Make). Entity names and enums must match exactly."*

---

## 7. Definition of Done — today's ERD deliverable

- [ ] Figma Make project **Griot** contains the ERD: all **16 tables** (13 core + ApiLogs/ErrorLogs/AuditLogs), **5 enums**, **19 labelled crow's-foot relationships**, legend, index notes
- [ ] Module color-coding applied (Identity/Auth, Core Board, Social, Observability, Audit)
- [ ] No entity without a Week-1 screen or observability requirement; names/values match §2 + `data-layer.md` exactly
- [ ] Refined ≥1 round; approved; PNG exported to `project-kit/diagrams/erd/griot-erd-v1.0.0.png`
- [ ] Diagrams ledger updated (source = Figma Make project URL)
- [ ] Backend feature 02 treats the ERD as its contract; no schema code before approval
