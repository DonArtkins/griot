# Week 02 · Prompt 02 — ERD in Figma Make: THE MASTER PROMPT (no length limit)

> 📌 **Paste this entire prompt into Figma Make.** There is **NO character limit here** — it is written to be complete, exhaustive, and detailed so the AI produces the full 16-table / 5-enum / 21-relationship / 19-index ERD in **one generation** (or in as many follow-ups as needed if the canvas tool truncates — each follow-up repeats the missing table by name).
> **Tool:** Figma Make, live project: `https://www.figma.com/make/bTB7eE6s39O6yvtYfq0DeR/Griot`
> **Output:** `diagrams/erd/griot-erd-v1.0.0.png`

---

## ⚡ THE PROMPT — paste exactly this (start)

```text
Design a complete, production-grade entity-relationship diagram (ERD) for **Griot**, a project-management web application with an AI copilot. Target database: **SQL Server 2022**. This ERD is the implementation contract for an Entity Framework Core 8 data layer, so every table name, column name, type, key, and relationship must match EXACTLY. Enums are listed as a legend panel, not tables.

## GLOBAL STYLE (apply to the entire diagram)
- Crow's-foot notation. Dark header rows with white bold PascalCase table names. One row per column: `Name · Type · badges` where badges are PK (primary key), FK (foreign key), UQ (unique), ? (nullable).
- Group tables by module and COLOR-CODE the header:
  - Identity/Auth = purple: Users, RefreshTokens, WorkspaceMembers, Invites
  - Core Board = blue: Workspaces, Projects, Boards, Columns, TaskItems
  - Social = teal: Comments, Attachments
  - Observability = amber: ActivityLogs, Notifications, ApiLogs, ErrorLogs, AuditLogs
- Add a legend panel in the top-left: PK = key icon, FK = arrow, UQ = UQ badge, ? = nullable.
- Add an INDEXES column of sticky notes on the right side (index list below).
- Keep everything readable at 100% zoom; orthogonal edge routing; no overlapping edges.

## ENUMS (legend panel, top-right)
- TaskStatus: Backlog · Todo · InProgress · InReview · Done
- Priority: Low · Medium · High · Urgent
- WorkspaceRole: Owner · Admin · Member
- NotificationType: Mention · Assignment · DueDate · System
- ErrorFixStatus: Open · Investigating · Fixed · Verified · WontFix
- TwoFactorMethod: None · EmailOtp · Totp
- InviteStatus: Pending · Accepted · Declined · Expired  *(inline on Invites.Status — not a separate EF Core enum)*
- ProjectStatus: Active · Archived  *(inline on Projects.Status — not a separate EF Core enum)*

## TABLES — 18 total (exact columns)

### Users (Identity)
Id GUID PK | Email nvarchar(320) UQ | DisplayName nvarchar(100) | AvatarUrl nvarchar(500) ? | PasswordHash nvarchar(512) | TwoFactorMethod TwoFactorMethod | CreatedAt datetime2 | UpdatedAt datetime2

### Workspaces (Core)
Id GUID PK | Name nvarchar(100) | Slug nvarchar(100) UQ | OwnerId FK→Users | CreatedAt datetime2 | UpdatedAt datetime2

### WorkspaceMembers (Identity join)
WorkspaceId FK→Workspaces PK | UserId FK→Users PK | Role WorkspaceRole | JoinedAt datetime2

### Invites (Identity)
Id GUID PK | WorkspaceId FK→Workspaces | Email nvarchar(320) | Role WorkspaceRole | Token nvarchar(128) UQ | Status InviteStatus(Pending·Accepted·Declined·Expired) | InvitedById FK→Users | ExpiresAt datetime2 | CreatedAt datetime2

### Projects (Core)
Id GUID PK | WorkspaceId FK→Workspaces | Name nvarchar(150) | Key nvarchar(10) | Description nvarchar(max) ? | Status ProjectStatus(Active·Archived) | CreatedAt datetime2 | UpdatedAt datetime2

### Boards (Core)
Id GUID PK | ProjectId FK→Projects | Name nvarchar(100) | Order int | CreatedAt datetime2

### Columns (Core)
Id GUID PK | BoardId FK→Boards | Name nvarchar(100) | Order int | WipLimit int ? | CreatedAt datetime2

### TaskItems (Core)
Id GUID PK | BoardId FK→Boards | ColumnId FK→Columns | Title nvarchar(200) | Description nvarchar(max) ? | Status TaskStatus | Priority Priority | AssigneeId FK→Users ? | CreatorId FK→Users | DueDate date ? | Position decimal | CreatedAt datetime2 | UpdatedAt datetime2

### Comments (Social)
Id GUID PK | TaskId FK→TaskItems | AuthorId FK→Users | Body nvarchar(max) | CreatedAt datetime2 | UpdatedAt datetime2

### Attachments (Social)
Id GUID PK | TaskId FK→TaskItems | UploaderId FK→Users | FileName nvarchar(255) | MimeType nvarchar(100) | SizeBytes bigint | StorageUrl nvarchar(500) | CreatedAt datetime2
### OtpChallenges (Identity)
Id GUID PK | UserId FK→Users | CodeHash nvarchar(128) | Purpose nvarchar(50) | ExpiresAt datetime2 | AttemptCount int | Consumed bit | CreatedAt datetime2 | RequestIp nvarchar(45) ?

### Reports (Core)
Id GUID PK | WorkspaceId FK→Workspaces | Type nvarchar(50) | GeneratedBy nvarchar(100) | ContentJson nvarchar(max) JSON | GeneratedAt datetime2 | PromptContext nvarchar(max) ?
```

### ⚡ PROMPT (continue — observability tables)

```text
### ActivityLogs (Observability)
Id GUID PK | WorkspaceId FK→Workspaces | ActorId FK→Users | EntityType nvarchar(50) | EntityId GUID | Action nvarchar(50) | Payload nvarchar(max) JSON ? | CreatedAt datetime2

### Notifications (Observability)
Id GUID PK | UserId FK→Users | Type NotificationType | Title nvarchar(200) | Body nvarchar(500) ? | TargetRef nvarchar(200) | ReadAt datetime2 ? | CreatedAt datetime2

### RefreshTokens (Identity)
Id GUID PK | UserId FK→Users | TokenHash nvarchar(128) UQ | FamilyId GUID (stable rotation family) | ExpiresAt datetime2 | RevokedAt datetime2 ? | ReplacedByTokenId GUID ? | CreatedAt datetime2

### ApiLogs (Observability — request tracing)
Id GUID PK | RequestId GUID UQ | UserId FK→Users ? | Method nvarchar(8) | Path nvarchar(300) | QueryString nvarchar(500) ? | StatusCode int | DurationMs int | UserAgent nvarchar(300) ? | IpAddress nvarchar(45) ? | CreatedAt datetime2

### ErrorLogs (Observability — error tracking)
Id GUID PK | RequestId GUID ? | UserId FK→Users ? | ExceptionType nvarchar(200) | Message nvarchar(max) | StackTrace nvarchar(max) ? | Source nvarchar(100) ? | FixStatus ErrorFixStatus | SolvedByUserId FK→Users ? | FixedAt datetime2 ? | CreatedAt datetime2

### AuditLogs (Observability — change history)
Id GUID PK | ActivityId FK→ActivityLogs ? | ActorId FK→Users | Action nvarchar(50) | EntityType nvarchar(50) | EntityId GUID | Before nvarchar(max) JSON ? | After nvarchar(max) JSON ? | CreatedAt datetime2

OBSERVABILITY GOVERNANCE (annotation box connected to all three amber tables):
"Retention: ApiLogs/ActivityLogs/AuditLogs = 90-day hot window, pruned monthly. ErrorLogs = keep Open/Investigating indefinitely; prune Fixed after 90 days from FixedAt. PII/secrets redaction before write: IpAddress last-octet masked; QueryString credentials stripped at middleware; Message/StackTrace secrets scrubbed by logging middleware; JSON Payload/Before/After must never contain plaintext passwords or tokens. Append-only — no UPDATE/DELETE on these tables."
```

### ⚡ PROMPT (continue — relationships, indexes, conventions)

```text
## RELATIONSHIPS — 21 edges, label every edge with the FK name
1. WorkspaceMembers.WorkspaceId → Workspaces.Id (1:N) + WorkspaceMembers.UserId → Users.Id (1:N) [M:N join Users↔Workspaces, Role on association]
2. Workspaces.OwnerId → Users.Id (N:1)
3. Workspaces 1:N WorkspaceMembers (cascade members)
4. Workspaces 1:N Projects
5. Workspaces 1:N ActivityLogs
6. Workspaces 1:N Invites
7. Users 1:N RefreshTokens (rotation chain via ReplacedByTokenId)
8. Users 1:N Notifications (ReadAt nullable = unread)
9. Users 1:N ApiLogs (nullable)
10. Users 1:N ErrorLogs (nullable, via UserId)
11. ErrorLogs.SolvedByUserId → Users.Id (N:1 nullable) — who resolved the error — draw SEPARATELY from #10
12. AuditLogs.ActorId → Users.Id (N:1 required) — who performed the audited action
13. ActivityLogs 1:N AuditLogs (optional: AuditLogs.ActivityId → ActivityLogs.Id)
14. Projects 1:N Boards
15. Boards 1:N Columns (ordered)
16. Columns 1:N TaskItems (Position within column)
17. TaskItems.AssigneeId → Users.Id (N:1 nullable) — draw SEPARATELY
18. TaskItems.CreatorId → Users.Id (N:1 required) — draw SEPARATELY
19. TaskItems 1:N Comments (cascade)
20. TaskItems 1:N Attachments (cascade)
21. Invites.InvitedById → Users.Id (N:1 required) — who sent the invite

## INDEXES — sticky-note column on the right (19 index stickies total)
- Every FK column → non-clustered index (1 rule sticky covering all FKs)
- TaskItems(ColumnId, Position) — board read + drag hot path
- TaskItems(AssigneeId), TaskItems(DueDate) — my tasks + reminder agent
- ActivityLogs(WorkspaceId, CreatedAt DESC) — feed + summarize_project
- Notifications(UserId, ReadAt) — unread count
- RefreshTokens(UserId), RefreshTokens(TokenHash) UQ, RefreshTokens(UserId, FamilyId)
- Invites(Token) UQ, Invites(WorkspaceId, Email)
- ApiLogs(RequestId) UQ, ApiLogs(UserId, CreatedAt), ApiLogs(Path)
- ErrorLogs(FixStatus) partial, ErrorLogs(FixedAt)
- AuditLogs(ActorId), AuditLogs(ActivityId)
- Users.Email UQ, Workspaces.Slug UQ

## CONVENTIONS NOTE (small annotation box)
- GUID PKs; timestamps SYSUTCDATETIME() (UTC, never GETDATE()); enums stored as varchar via HasConversion<string>() so SQL reads are readable; nvarchar(max) only for bodies/JSON; audit + error trails append-only; soft-delete NOT used (hard delete + AuditLog tombstone).
- Observability retention: ApiLogs/ActivityLogs/AuditLogs 90-day hot window, pruned monthly; ErrorLogs Open/Investigating kept indefinitely, Fixed pruned 90d after FixedAt.
- PII/secrets redaction: IpAddress last-octet masked; QueryString credentials stripped at middleware; Message/StackTrace scrubbed by logging middleware; JSON Payload/Before/After must never contain plaintext passwords or tokens.
```

---

## If the tool truncates (safety)

Paste the full prompt once. If the canvas omits any table (named above), **do not re-paste everything** — follow up with one short add-on per missing item, e.g.: `"Add table TaskItems with the exact columns listed in my first prompt."` Use the master as the single source; fix snippets below recover any drift.

## Fix snippets

- "In table X rename column A→B; add C nullable; drop D."
- "Recolor headers of all Core Board tables to blue."
- "Redraw WorkspaceMembers as a join box between Users and Workspaces, Role on the association line."
- "Draw TaskItems.CreatorId and TaskItems.AssigneeId as two separate edges to Users."
- "Move the Indexes stickies to a single right-hand column."

## Definition of Done (this ERD)

- [ ] 18 tables + 6 enums + 21 labelled crow's-foot edges + 19 index stickies + legend + conventions note, all on one canvas
- [ ] Names/values match `backend/project-kit/context/data-layer.md` + `docs/database/DATABASE-DESIGN.md` exactly
- [ ] Module color-coding (purple/blue/teal/amber) applied
- [ ] Approved → PNG → `diagrams/erd/griot-erd-v1.0.0.png` → ledger updated

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

The current REST transport uses JSON refresh tokens for Postman/mobile. Web
HttpOnly cookie transport in the design remains a backend prerequisite for web
Feature 05; do not treat the cookie diagrams as live behavior or store tokens in
localStorage. SQL Server owns refresh rows; Redis currently owns login limits.
