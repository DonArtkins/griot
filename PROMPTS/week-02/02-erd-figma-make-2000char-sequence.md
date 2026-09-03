# Week 02 · Prompt Set — ERD in Figma Make (2000-char limit, sequenced)

Why: Figma Make caps AI prompts at **2000 chars**. 13 tables + 4 enums + 15 edges + 7 indexes will NOT fit in one prompt. Solution — SEQUENCE on the same canvas: Prompt A seeds the diagram, then B–F ADD/UPDATE the live canvas (Figma Make supports follow-up edits). Works. Each prompt ≤2000 chars, compact, paste-ready. Order: A seed → B core → C social → D enums+edges → E indexes+legend → F verify.

---

### PROMPT A — Seed (4 tables, Identity/Auth)

```
ERD for Griot, a PM web app. Target SQL Server 2022. Crow's foot. Dark headers; rows show name·type·badges (PK,FK,UQ,? nullable). Build only these 4 tables; purple=Identity/Auth module:
Users:Id GUID PK|Email nvarchar(320) UQ|DisplayName nvarchar(100)|AvatarUrl?|PasswordHash nvarchar(512)|CreatedAt datetime2|UpdatedAt
Workspaces:Id GUID PK|Name nvarchar(100)|Slug nvarchar(100) UQ|OwnerId FK→Users|CreatedAt|UpdatedAt
WorkspaceMembers:WorkspaceId FK PK|UserId FK PK|Role WorkspaceRole|JoinedAt
Invites:Id GUID PK|WorkspaceId FK→Workspaces|Email nvarchar(320)|Role WorkspaceRole|Token nvarchar(128) UQ|Status Pending Accepted Declined Expired|InvitedById FK→Users|ExpiresAt|CreatedAt
Edges:WorkspaceMembers=Users M:N Workspaces (Role on join);Workspaces.OwnerId→Users;Workspaces 1:N WorkspaceMembers;Workspaces 1:N Invites;Users 1:N Workspaces(Owner).
```

### PROMPT B — Core Board (4 tables, blue)

```
Add 4 tables to the Griot ERD, same style; blue=Core Board:
Projects:Id GUID PK|WorkspaceId FK→Workspaces|Name nvarchar(150)|Key nvarchar(10)|Description nvarchar(max)?|Status Active Archived|CreatedAt|UpdatedAt
Boards:Id GUID PK|ProjectId FK→Projects|Name nvarchar(100)|Order int|CreatedAt
Columns:Id GUID PK|BoardId FK→Boards|Name nvarchar(100)|Order int|WipLimit int?|CreatedAt
TaskItems:Id GUID PK|BoardId FK→Boards|ColumnId FK→Columns|Title nvarchar(200)|Description nvarchar(max)?|Status TaskStatus|Priority Priority|AssigneeId FK→Users?|CreatorId FK→Users|DueDate date?|Position decimal|CreatedAt|UpdatedAt
Edges:Workspaces 1:N Projects;Projects 1:N Boards;Boards 1:N Columns;Columns 1:N TaskItems;TaskItems→Users twice (AssigneeId ? , CreatorId required).
```

### PROMPT C — Social + Observability + Refresh (5 tables)

```
Add 5 tables to the Griot ERD, same style. Teal=Social: Comments,Attachments. Amber=Observability: ActivityLogs,Notifications. Purple=Identity: RefreshTokens.
Comments:Id GUID PK|TaskId FK→TaskItems|AuthorId FK→Users|Body nvarchar(max)|CreatedAt|UpdatedAt
Attachments:Id GUID PK|TaskId FK→TaskItems|UploaderId FK→Users|FileName nvarchar(255)|MimeType nvarchar(100)|SizeBytes bigint|StorageUrl nvarchar(500)|CreatedAt
ActivityLogs:Id GUID PK|WorkspaceId FK→Workspaces|ActorId FK→Users|EntityType nvarchar(50)|EntityId GUID|Action nvarchar(50)|Payload nvarchar(max) JSON?|CreatedAt
Notifications:Id GUID PK|UserId FK→Users|Type NotificationType|Title nvarchar(200)|Body nvarchar(500)?|TargetRef nvarchar(200)|ReadAt datetime2?|CreatedAt
RefreshTokens:Id GUID PK|UserId FK→Users|TokenHash nvarchar(128) UQ|ExpiresAt|RevokedAt?|ReplacedByTokenId GUID?|CreatedAt
Edges:TaskItems 1:N Comments cascade;TaskItems 1:N Attachments cascade;Users 1:N Notifications;Users 1:N RefreshTokens (rotation chain ReplacedByTokenId);Workspaces 1:N ActivityLogs.
```

### PROMPT D — Enums + edge check

```
Add enum legend panel to the Griot ERD (values fixed, tables already drawn):
TaskStatus:Backlog Todo InProgress InReview Done
Priority:Low Medium High Urgent
WorkspaceRole:Owner Admin Member
NotificationType:Mention Assignment DueDate System
Verify every edge exists; add missing: Users M:N Workspaces via WorkspaceMembers(Role on association);Workspaces.OwnerId→Users;Workspaces 1:N Projects,WorkspaceMembers,Invites,ActivityLogs;Users 1:N RefreshTokens,Notifications;Projects 1:N Boards;Boards 1:N Columns;Columns 1:N TaskItems;TaskItems→Users x2(AssigneeId?,CreatorId);TaskItems 1:N Comments,Attachments.
Label each edge with FK name, e.g. TaskItems.AssigneeId→Users.Id. Keep 100% zoom, no overlap, orthogonal routing.
```

### PROMPT E — Indexes + legend

```
Add blue sticky-note column "Indexes" beside the Griot ERD tables:
every FK non-clustered
TaskItems(ColumnId,Position)
TaskItems(AssigneeId)
TaskItems(DueDate)
ActivityLogs(WorkspaceId,CreatedAt DESC)
Notifications(UserId,ReadAt)
RefreshTokens(TokenHash) UQ
RefreshTokens(UserId)
Invites(Token) UQ
Invites(WorkspaceId,Email)
Users.Email UQ
Workspaces.Slug UQ
Workspaces.OwnerId
Add legend top-left: PK=key icon,FK=arrow,UQ=badge,?=nullable. Enums panel bottom-right. Reflow so nothing overlaps.
```

### PROMPT F — Verify (run last; fix with snippets below)

```
Check the Griot ERD: exactly 16 tables (Users Workspaces WorkspaceMembers Invites Projects Boards Columns TaskItems Comments Attachments ActivityLogs Notifications RefreshTokens ApiLogs ErrorLogs AuditLogs); 5 enums (TaskStatus Priority WorkspaceRole NotificationType ErrorFixStatus); 19 edges; every entity traces to a Week-1 screen or observability requirement; names match the spec exactly. Report missing items so I restate them.
```

> Note: prompts G + H create the observability/audit tables (ApiLogs, ErrorLogs, AuditLogs) that let us track errors + full change history — so we never come back to redesign the schema later. Run them after A–F, before approval.

### PROMPT G — Observability + audit tables (amber)

```
Add 3 tables to the Griot ERD, same style, amber=Observability:
ApiLogs:Id GUID PK|RequestId GUID UQ|UserId FK→Users?|Method nvarchar(8)|Path nvarchar(300)|QueryString nvarchar(500)?|StatusCode int|DurationMs int|UserAgent nvarchar(300)?|IpAddress nvarchar(45)?|CreatedAt
ErrorLogs:Id GUID PK|RequestId GUID?|UserId FK→Users?|ExceptionType nvarchar(200)|Message nvarchar(max)|StackTrace nvarchar(max)?|Source nvarchar(100)?|FixStatus ErrorFixStatus|SolvedByUserId FK→Users?|FixedAt datetime2?|CreatedAt
AuditLogs:Id GUID PK|ActivityId FK→ActivityLogs?|ActorId FK→Users|Action nvarchar(50)|EntityType nvarchar(50)|EntityId GUID|Before nvarchar(max) JSON?|After nvarchar(max) JSON?|CreatedAt
Edges:Users 1:N ApiLogs;Users 1:N ErrorLogs;ActivityLogs 1:N AuditLogs (optional). Purpose: request tracing, error fix tracking, change history.
```

### PROMPT H — observability indexes

```
Add to the Indexes column on the Griot ERD:
ApiLogs(RequestId) UQ
ApiLogs(UserId,CreatedAt)
ApiLogs(Path)
ErrorLogs(FixStatus) partial
ErrorLogs(FixedAt) for pruning
AuditLogs(ActorId)
AuditLogs(ActivityId)
Add enum ErrorFixStatus:Open Investigating Fixed Verified WonTFix to the enums panel.
```

### Fix snippets (paste after any drift)

- "In table X rename A→B; change type to C; add D nullable; drop E."
- "Add to TaskItems: DueDate date ? after Priority."
- "Header color of X → purple Identity module."
- "Draw WorkspaceMembers as join Users↔Workspaces, Role on the association line."
- "Replace edge X→Y label with X.Field→Y.Id; arrow on Y side."
- "Add missing table: <table>:<fields>"
- "Recolor all Core Board headers blue."

### After approval

Export PNG → `project-kit/diagrams/erd/griot-erd-v1.0.0.png` → update `project-kit/diagrams/README.md` ledger → backend feature 02 transcribes names verbatim.