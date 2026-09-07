# Week 02 · Prompt 01 — Database Schema Design in Figma Make (ERD)

> 📌 **This is today's deliverable — the first item of Week 2 in the GTP 2026 roadmap: "Database Schema Design in Figma."**
> **Tool:** Figma Make (the AI prototyping surface), live project:
> `https://www.figma.com/make/bTB7eE6s39O6yvtYfq0DeR/Griot?p=f&t=CrPqLvvqtlfVbwSx-0`
> **Output lives at:** `diagrams/erd/` (export a PNG frame into that folder — Feature 02 and every schema decision reads it).
> **Research input:** `research/week-01-fundamentals-and-system-design.md` §4.4 (screen → entity table) + `research/week-02-backend-api-development.md` §3.

---

## 1. What we are building

Griot is a project-management web app (workspaces → projects → boards → columns → tasks) with comments, attachments, roles/invites, an activity feed, and notifications. The Week-1 Figma screens defined the entities; **this ERD is the translation of those screens into a relational schema for SQL Server 2022**, which the EF Core data layer (backend feature 02) will then transcribe 1:1.

**Guarding rule:** every entity below traces to either a **Week-1 screen** (e.g. Dashboard → ActivityLog, Board → Column/TaskItem, Task detail → Comment/Attachment, Team settings → WorkspaceMember/Invite, Notifications → Notification) **or an explicit observability/audit requirement** (ApiLogs, ErrorLogs, AuditLogs — added in planning so the schema never changes later). An entity with neither justification is not in this ERD.

## 2. The entity roster (18 tables + 6 enums)

Derived from the Week-1 entity/screen mapping + the Lyncxs observability/error-tracking conventions. Use **exactly** these names, types, and relationships. These names ARE the contract for `Griot.Domain` entities, GraphQL types, TS/Dart models, and MCP tool schemas. Adding `ApiLogs`/`ErrorLogs`/`AuditLogs` now (before code) means we never come back to redesign the schema later.

### Enums (draw as a legend panel, v1 values fixed)
| Enum | Values |
|---|---|
| `TwoFactorMethod` | EmailOtp · Totp · None |
| `TaskStatus` | Backlog · Todo · InProgress · InReview · Done |
| `Priority` | Low · Medium · High · Urgent |
| `WorkspaceRole` | Owner · Admin · Member |
| `NotificationType` | Mention · Assignment · DueDate · System |
| `ErrorFixStatus` | Open · Investigating · Fixed · Verified · WontFix (for ErrorLogs) |

### Tables

**OtpChallenges**
`Id` (PK), `UserId` (FK), `CodeHash`, `Purpose`, `ExpiresAt`, `AttemptCount`, `Consumed`, `CreatedAt`, `RequestIp`

**Reports**
`Id` (PK), `WorkspaceId` (FK), `Type`, `GeneratedBy`, `ContentJson`, `GeneratedAt`, `PromptContext`
**Users** — `Id` (GUID, PK) · `Email` (nvarchar(320), UQ) · `DisplayName` (nvarchar(100)) · `AvatarUrl` (nvarchar(500), nullable) · `PasswordHash` (nvarchar(512), Argon2) · `CreatedAt` (datetime2, SYSUTCDATETIME) · `UpdatedAt`

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

**Governance:** ApiLogs, ActivityLogs, and AuditLogs have a **90-day hot retention window** (archive/prune monthly per `docs/database/DATABASE-DESIGN.md §7`). ErrorLogs retain Open/Investigating records indefinitely; Fixed records are pruned after 90 days from `FixedAt`. **PII/secrets redaction is mandatory before write:** `IpAddress` is masked (last octet zeroed or hashed), `QueryString` must have credentials/tokens stripped at the middleware level, `UserAgent` is stored verbatim (no PII). `Message` and `StackTrace` in ErrorLogs must be scrubbed of secrets by the logging middleware before persistence. JSON `Payload`/`Before`/`After` fields must never contain plaintext passwords, tokens, or payment data — enforce via a sanitiser hook in `SaveChangesAsync`.

**ApiLogs** — `Id` (GUID, PK) · `RequestId` (GUID, UQ — correlation/tracing) · `UserId` (FK → Users, nullable) · `Method` (nvarchar(8)) · `Path` (nvarchar(300)) · `QueryString` (nvarchar(500), nullable — secrets stripped) · `StatusCode` (int) · `DurationMs` (int) · `UserAgent` (nvarchar(300), nullable) · `IpAddress` (nvarchar(45), nullable — last-octet masked) · `CreatedAt`

**ErrorLogs** — `Id` (GUID, PK) · `RequestId` (GUID, nullable) · `UserId` (FK → Users, nullable) · `ExceptionType` (nvarchar(200)) · `Message` (nvarchar(max) — secrets scrubbed) · `StackTrace` (nvarchar(max), nullable — secrets scrubbed) · `Source` (nvarchar(100), nullable — Api/GraphQL/Agent/Worker) · `FixStatus` (enum ErrorFixStatus, default Open) · `SolvedByUserId` (FK → Users, nullable) · `FixedAt` (datetime2, nullable) · `CreatedAt`

**AuditLogs** — `Id` (GUID, PK) · `ActivityId` (FK → ActivityLogs, nullable) · `ActorId` (FK → Users) · `Action` (nvarchar(50)) · `EntityType` (nvarchar(50)) · `EntityId` (GUID) · `Before` (nvarchar(max) JSON, nullable — snapshot pre-change; no secrets) · `After` (nvarchar(max) JSON, nullable — snapshot post-change; no secrets) · `CreatedAt`

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
| Users | ErrorLogs (Solver) | N:1 nullable | `ErrorLogs.SolvedByUserId` — who resolved the error |
| ActivityLogs | AuditLogs | 1:N | `ActivityId` optional link |
| Users | AuditLogs (Actor) | N:1 | `AuditLogs.ActorId` — who performed the audited action |
| Users | Invites (InvitedBy) | N:1 | `Invites.InvitedById` — who sent the invite |

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

> Use the **master prompt file** (`02-erd-figma-make-master-prompt.md`) for building in Figma Make — one extensive prompt covering all 18 tables, 6 enums, 21 relationships, and 19 indexes (including FK index coverage). If the canvas truncates, add any missing table with a short add-on prompt (name the table + "as in my first prompt").

---

## 5. The prompt (paste into Figma Make — no length limit)

Open your Figma Make project **Griot** (`https://www.figma.com/make/bTB7eE6s39O6yvtYfq0DeR/Griot`), then paste the full **master prompt** from `02-erd-figma-make-master-prompt.md` — one extensive prompt covering all 18 tables, 6 enums, 21 labelled crow's-foot relationships, 19 index stickies, and the SQL Server conventions note. Use **Plan mode** to steer before generating. If the canvas truncates, add any missing table via a short add-on prompt (name the table + "as in my first prompt") — never omit columns.

**Same authoritative content lives in `02-erd-figma-make-master-prompt.md`; that is the file agents/you paste from.** The shortened version below is the **quick sanity check** of the contract:

> **Entities (18):** `Users`, `Workspaces`, `WorkspaceMembers` (M:N join with `Role`), `Invites`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `ActivityLogs`, `Notifications`, `RefreshTokens`, `OtpChallenges`, `Reports`, `ApiLogs`, `ErrorLogs`, `AuditLogs` — exact field lists in §2 (GUID PKs, `nvarchar` types, FK labels, nullable `?`, unique `UQ`).
>
> **Enums (6):** `TaskStatus` Backlog·Todo·InProgress·InReview·Done · `Priority` Low·Medium·High·Urgent · `WorkspaceRole` Owner·Admin·Member · `NotificationType` Mention·Assignment·DueDate·System · `ErrorFixStatus` Open·Investigating·Fixed·Verified·WontFix · `TwoFactorMethod` None·EmailOtp·Totp. Legend panel.
>
> **Enums (inline, not separate tables):** `InviteStatus` Pending·Accepted·Declined·Expired (on `Invites.Status`) · `ProjectStatus` Active·Archived (on `Projects.Status`). These are single-table enums rendered inline on the ERD, not top-level legend entries.
>
> **Relationships (21):** WorkspaceMembers joins Users↔Workspaces (Role on association); Workspaces→Projects→Boards→Columns→TaskItems; Workspaces→Reports; TaskItems TWO FKs to Users (`AssigneeId?`, `CreatorId`); Comments/Attachments→TaskItems; Workspaces→ActivityLogs+Invites; Users→RefreshTokens (rotation chain)+Notifications+ApiLogs+ErrorLogs+OtpChallenges; `ErrorLogs.SolvedByUserId→Users` (nullable); `AuditLogs.ActorId→Users` (required); `Invites.InvitedById→Users`; ActivityLogs→(optional)AuditLogs.
>
> **Indexes (19 stickies):** every FK non-clustered (1 rule sticky); `TaskItems(ColumnId, Position)`; `TaskItems(AssigneeId)`; `TaskItems(DueDate)`; `ActivityLogs(WorkspaceId, CreatedAt DESC)`; `Notifications(UserId, ReadAt)`; `RefreshTokens(UserId)`, `RefreshTokens(TokenHash)` UQ; `Invites(Token)` UQ, `Invites(WorkspaceId, Email)`; `ApiLogs(RequestId)` UQ, `ApiLogs(UserId, CreatedAt)`, `ApiLogs(Path)`; `ErrorLogs(FixStatus)` partial, `ErrorLogs(FixedAt)`; `AuditLogs(ActorId)`, `AuditLogs(ActivityId)`; `Users.Email` UQ, `Workspaces.Slug` UQ.
>
> **Style:** color-code by module (Identity/Auth purple · Core Board blue · Social teal · Observability amber · Audit amber); legend (PK/FK/UQ/?); conventions note (GUID PKs, SYSUTCDATETIME, enums-as-varchar, append-only audit). Readable at 100% zoom; orthogonal routing; no overlaps.

## 6. Step-by-step: building the ERD in Figma Make

### Step 1 — Open the project
Open the live project: **https://www.figma.com/make/bTB7eE6s39O6yvtYfq0DeR/Griot?p=f&t=CrPqLvvqtlfVbwSx-0** (this is your Week-2 canvas inside the same Figma account used for the Week-1 design system).

### Step 2 — Use the AI Describe step (Plan mode)
1. Click into the Make canvas and open the **AI / Describe** field.
2. Choose **Plan mode** first: paste the §5 prompt, let Make outline its plan (tables + relationships), review that the plan matches the entity roster, then **Generate**.
3. If Make's draft misses a table or a field, tell it exactly what to add in a follow-up prompt before building by hand.

### Step 3 — Fix the tables by hand (precision pass)
Make's first pass is a *rough clay model*. On the Make canvas:
1. Arrange one **shape/table per entity** (16 total). Snap-guides keep them aligned.
2. Header = dark fill, white bold **PascalCase** name (`TaskItems`, `ActivityLogs`).
3. Fields one row each: `Id · GUID · PK`, `?` for nullable, `UQ` for unique, `→` for FKs.
4. Duplicate a polished table shape (`Ctrl/Cmd+D`) then retitle — keep visual rhythm identical.

### Step 4 — Color-code modules
Fill each header per module: Identity/Auth = purple · Core Board = blue · Social = teal · Observability = amber.

### Step 5 — Draw the 19 relationship edges
Use the **connector line** tool between node handles; set orthogonal/crisp routing; put an arrow on the "many" side; label each edge `TaskItems.AssigneeId → Users.Id` etc.

### Step 6 — Add legend, enums, index notes
1. **Legend** (top-left): PK / FK / UQ / nullable symbols.
2. **Enums panel** (right): the 5 enum chips/tables.
3. **Index notes** (bottom or right column): one sticky per index from §4, dashed-connected to its table.

### Step 7 — Refine (≤2 rounds)
Read it like a reviewer: does every Week-1 screen's entity appear? (Dashboard → Workspace/Project/ActivityLog; Board → Board/Column/TaskItem; Task detail → TaskItem + Comment + Attachment; Team settings → User + WorkspaceMember + Invite; Notifications → Notification.) No orphan table. Tidy overlaps; verify the 19 edges against §3.

### Step 8 — Approve + export
1. Human approves the ERD in the Make project.
2. Export the ERD frame as **PNG** → save as `griot-erd-v1.0.0.png` into `diagrams/erd/`.
3. Update `diagrams/README.md` ledger (name, version, date, status=✅ approved, source=Figma Make project URL).

### Step 9 — Feed it to the code agent
When prompting the backend agent (Cline/Claude), say: *"Build backend features 01–02 from the approved ERD `diagrams/erd/griot-erd-v1.0.0.png` (produced in Figma Make). Entity names and enums must match exactly."*

---

## 7. Definition of Done — today's ERD deliverable

- [ ] Figma Make project **Griot** contains the ERD: all **18 tables** (13 core + ApiLogs/ErrorLogs/AuditLogs), **6 enums**, **21 labelled crow's-foot relationships**, legend, index notes
- [ ] Module color-coding applied (Identity/Auth, Core Board, Social, Observability, Audit)
- [ ] No entity without a Week-1 screen or observability requirement; names/values match §2 + `data-layer.md` exactly
- [ ] Refined ≥1 round; approved; PNG exported to `diagrams/erd/griot-erd-v1.0.0.png`
- [ ] Diagrams ledger updated (source = Figma Make project URL)
- [ ] Backend feature 02 treats the ERD as its contract; no schema code before approval
