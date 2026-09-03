# Week 02 · Prompt 01 — Database Schema Design in Figma (ERD in FigJam)

> 📌 **This is today's deliverable — the first item of Week 2 in the GTP 2026 roadmap: "Database Schema Design in Figma."**
> **Where the output lives:** `project-kit/diagrams/erd/` (export the PNG into that folder — Feature 03 and every schema decision reads it).
> **Research input:** `research/week-01-fundamentals-and-system-design.md` §4.4 (screen → entity table) + `research/week-02-backend-api-development.md` §3.

---

## 1. What we are building

Griot is a project-management web app (workspaces → projects → boards → columns → tasks) with comments, attachments, roles/invites, an activity feed, and notifications. The Week-1 Figma screens defined the entities; **this ERD is the translation of those screens into a relational schema for SQL Server 2022**, which the EF Core data layer (Feature 03) will then transcribe 1:1.

**Guarding rule:** every entity below traces to a Week-1 screen. If it's not on a screen, it's not in this ERD.

## 2. The entity roster (13 tables + 4 enums)

Derived from the Week-1 entity/screen mapping. Use **exactly** these names, types, and relationships.

### Enums (draw as legend/chips, v1 values fixed)
| Enum | Values |
|---|---|
| `TaskStatus` | Backlog · Todo · InProgress · InReview · Done |
| `Priority` | Low · Medium · High · Urgent |
| `WorkspaceRole` | Owner · Admin · Member |
| `NotificationType` | Mention · Assignment · DueDate · System |

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

**ActivityLogs** (the feed + the AI audit trail) — `Id` (GUID, PK) · `WorkspaceId` (FK) · `ActorId` (FK → Users) · `EntityType` (nvarchar(50): Project/Task/Comment/…) · `EntityId` (GUID) · `Action` (nvarchar(50): Created/Updated/Moved/Commented/…) · `Payload` (nvarchar(max) JSON, nullable) · `CreatedAt`

**Notifications** — `Id` (GUID, PK) · `UserId` (FK → Users) · `Type` (enum NotificationType) · `Title` (nvarchar(200)) · `Body` (nvarchar(500), nullable) · `TargetRef` (nvarchar(200): task/comment reference for deep-linking) · `ReadAt` (datetime2, nullable) · `CreatedAt`

## 3. Relationships & cardinality (draw exactly this)

| From | To | Cardinality | Notes |
|---|---|---|---|
| Users | Workspaces | M:N via `WorkspaceMembers` | Join with Role attribute |
| Workspaces | Users (Owner) | N:1 | `Workspaces.OwnerId` |
| Workspaces | WorkspaceMembers | 1:N | Cascade delete members |
| Workspaces | Projects | 1:N | |
| Workspaces | ActivityLogs | 1:N | Feed + AI audit |
| Workspaces | Invites | 1:N | |
| Users | RefreshTokens | 1:N | Rotation chain via `ReplacedByTokenId` |
| Projects | Boards | 1:N | |
| Boards | Columns | 1:N | Ordered |
| Columns | TaskItems | 1:N | `ColumnId`; `Position` within column |
| TaskItems | Users (Assignee) | N:1 nullable | `AssigneeId` |
| TaskItems | Users (Creator) | N:1 | `CreatorId` |
| TaskItems | Comments | 1:N | Cascade |
| TaskItems | Attachments | 1:N | Cascade |
| Users | Notifications | 1:N | `ReadAt` nullable = unread |

## 4. Indexes to annotate (draw as a note per table)

- Every FK column → non-clustered index.
- `TaskItems(ColumnId, Position)` — board read and drag-reorder hot path.
- `TaskItems(AssigneeId)` — "my tasks"; `TaskItems(DueDate)` — reminder agent query.
- `ActivityLogs(WorkspaceId, CreatedAt DESC)` — feed; also the AI `summarize_project` source.
- `Notifications(UserId, ReadAt)` — unread count.
- `RefreshTokens(UserId)`, `RefreshTokens(TokenHash)` unique — rotation lookup.
- `Invites(Token)` unique; `Invites(WorkspaceId, Email)` — prevent duplicate pending invites.

---

## 5. The prompt (paste-ready)

> **Context for the diagram tool:** I am designing the relational database schema for **Griot**, a project-management web app. The target database is **SQL Server 2022**. This diagram is the implementation contract for an Entity Framework Core data layer, so entity names, field names, types, and relationships must match the spec below exactly. Enums are named values, not tables.
>
> **Task:** Create an entity-relationship diagram (crow's-foot notation) with 13 entity tables and 4 enum labels.
>
> **Format:**
> - Each entity = a header-striped frame/table: dark header with the entity name (PascalCase), then one row per field showing `name  ·  type  ·  PK/FK/UQ/nullable` badges.
> - Group the tables by module and color-code the groups: **Identity/Auth** (Users, RefreshTokens, WorkspaceMembers, Invites), **Core Board** (Workspaces, Projects, Boards, Columns, TaskItems), **Social** (Comments, Attachments), **Observability** (ActivityLogs, Notifications).
> - Connect relationships with labelled edges using crow's foot: `1 → N` on the "many" side; label each edge with the FK name (e.g. `TaskItems.AssigneeId → Users.Id`).
> - Mark the two many-to-many-style joins explicitly: `Users M:N Workspaces via WorkspaceMembers(WorkspaceId, UserId)` with `Role` on the association.
> - Show the `TaskItems` relationships to Users carefully: it has two FKs to Users (`AssigneeId` nullable, `CreatorId` required) — draw both.
> - Add a legend: PK = key icon, FK = arrow, UQ = `UQ` badge, nullable = `?`.
> - Add the four enum definitions as a separate legend panel so values are visible without reading code.
> - Add an "Indexes" sticky-notes column beside the tables listing the annotated indexes above.
> - Keep the layout readable at 100% zoom on a wide board; no overlapping edges; orthogonal connector routing.

## 6. Step-by-step: how to build this ERD in FigJam

### What FigJam actually is
**FigJam** is Figma's *collaborative whiteboard/diagramming* surface (the "poor man's Miro" inside Figma). It has sticky notes, shapes, connectors, and — yes — an **AI button**. The traditional Figma desktop app is for high-fidelity UI; you do **not** need it for an ERD. FigJam is the right tool: schematic, fast, and exportable. A regular `Figma design file` also works with shapes + connectors, but FigJam's tables/connectors and AI make it faster.

### Step 1 — Open a FigJam file
1. Go to `figma.com` and sign in (the account from Week 1).
2. Click the **+ / New file** button in the top-right.
3. Choose **"New FigJam file"** (if you only see "Design file", use FigJam from the file-type dropdown).
4. Name it: **`Griot — Database Schema (ERD) — Week 2`**.

### Step 2 — (Optional) warm up FigJam AI
FigJam's top toolbar has an **AI sparkle button** ("Ask FigJam AI"). You can paste the prompt from §5 there and let it draft tables/stickies, then rearrange. AI drafts are **fast but imprecise** — treat them as a starting layout, then fix names/types by hand (Step 3–6) so the diagram is an exact contract. If AI isn't available in your region/plan, skip straight to manual — the steps below are fully manual and precise.

### Step 3 — Create the entity tables
1. Click the **shapes tool** (or press `S`), drop a **rectangle** for each of the 13 tables. FigJam snap-guides keep them aligned.
2. Better: **insert a table** — FigJam has a *Table* in the "Shapes & chart" library (Insert → Shapes & lines → Table). Use a table with an extra-long header row.
3. **Header**: dark fill, white bold text, entity name in **PascalCase** (`Users`, `TaskItems`, `ActivityLogs` …).
4. **Field rows**: one row per field, format `Id  ·  GUID  ·  PK` (keep it terse — type is a hint, not the DDL). Use ✓/key icon for `PK`, `UQ` for unique, `?` for nullable, arrow `→` for FK.
5. Duplicate the table shape 12 more times (select + `Ctrl/Cmd + D` keeps alignment) and rename headers.

### Step 4 — Color-code the modules
Use the **fill color** swatch to tint each table's header per module:

| Group | Color suggestion | Tables |
|---|---|---|
| Identity/Auth | Purple | Users, RefreshTokens, WorkspaceMembers, Invites |
| Core Board | Blue | Workspaces, Projects, Boards, Columns, TaskItems |
| Social | Teal | Comments, Attachments |
| Observability | Amber | ActivityLogs, Notifications |

### Step 5 — Draw the relationships
1. Use the **Connector** tool (`C`) or drag from a node's **white dot** to another node.
2. Set **line style** to *crisp/orthogonal* (Connector options → Line style → Orthogonal) so edges don't tangle.
3. Add an arrowhead on the "many" side → this is your crow's foot (FigJam arrows support single/multiple heads).
4. **Label each edge** with a text sticky or connector label: `TaskItems.AssigneeId → Users.Id`.
5. Draw all 15 relationships from §3 — including both `TaskItems → Users` edges (Assignee with `?`, Creator without).

### Step 6 — Add legend, enums, and index notes
1. **Legend** (top-left corner): small sticky notes defining PK / FK / UQ / nullable symbols.
2. **Enums panel** (right side): 4 sticky groups with the values from §2 (TaskStatus, Priority, WorkspaceRole, NotificationType).
3. **Index notes** (bottom or right column): one sticky per index from §4, attached to its table with a dashed connector.

### Step 7 — Refine (up to 2 rounds)
1. Read the diagram like a reviewer: does every Week-1 screen's entity appear? (Dashboard → Workspace/Project/ActivityLog; Board → Board/Column/TaskItem; etc.)
2. Check the **guarding rule**: no table that has no screen behind it.
3. Tidy overlaps; zoom to 100% and confirm everything is legible; re-read §3 vs. your edges.

### Step 8 — Export and archive
1. Select everything (`Ctrl/Cmd + A`), then **File → Export as PNG** (or right-click → Copy).
2. Save as **`griot-erd-v1.0.0.png`** into `project-kit/diagrams/erd/`.
3. Update the **diagrams README** (`project-kit/diagrams/README.md`) — file name, version, date, approved status.

### Step 9 — Feed it to the code agent
The approved ERD is the contract for `project-kit/feature-specs/03-database-schema-ef-core-sql-server.md`. When you prompt Cline/Claude for the schema, say: *"Build Feature 03 from the approved ERD at `project-kit/diagrams/erd/griot-erd-v1.0.0.png` and `PROMPTS/week-02/01-database-schema-erd-figma.md`."* Entity names and enums must match **exactly**.

---

## 7. Definition of Done — today's ERD deliverable

- [ ] FigJam file named `Griot — Database Schema (ERD)` contains all 13 tables, 4 enums, 15 labelled relationships (crow's foot), legend, index notes
- [ ] Module color-coding applied (Identity/Auth, Core Board, Social, Observability)
- [ ] No entity without a Week-1 screen behind it; names/values match §2 exactly
- [ ] Refined ≥1 round; PNG exported to `project-kit/diagrams/erd/griot-erd-v1.0.0.png`
- [ ] Diagrams README updated with the new artifact + approval status
- [ ] Feature 03 reads the ERD as its contract; no schema code written before approval