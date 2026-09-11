# ERD Amendment v2 — Multi-Tenancy (PLANNED — pending Figma Make approval)

**Status:** PLANNED design artifact. Per hard rule 4 (ERD before schema), no `Organizations`-related migration code may exist until this amendment is transcribed into the approved Figma Make ERD (root AGENTS.md rule 4) and exported to `diagrams/erd/`.
**Owner:** backend spec 29 (`feature-specs/29-multi-tenant-foundation-organizations.md`). Canonical field list: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §2.
**Precedent:** this follows the same amendment pattern as `diagrams/erd/auth-family-amendment.md` (RefreshTokens family columns), which was approved without re-opening the full v1.0.0 ERD.

## New entities (9)

1. `Organizations` — tenant root. PK `Id` (uuid). `Name`, `Slug` (unique, `UX_Organizations_Slug`), `OwnerId` → `Users.Id`, `Status` (`OrganizationStatus`), `Plan` (`OrganizationPlan`), `CreatedAt`, `UpdatedAt`, `OffboardedAt?`. Index: `IX_Organizations_OwnerId`.
2. `OrganizationMembers` — PK `Id`. FK `OrganizationId` → `Organizations.Id` (cascade), FK `UserId` → `Users.Id`. `Role` (`OrganizationRole`), `CustomRoleId?` → `Roles.Id` (nullable; only valid when `Role == Custom`), `Status` (member status: `Invited`/`Active`/`Suspended`), `JoinedAt`. Unique index `UX_OrganizationMembers_Org_User (OrganizationId, UserId)`; index `IX_OrganizationMembers_UserId`.
3. `Roles` — PK `Id`. FK `OrganizationId` → `Organizations.Id` (cascade). `Name`, `IsSystem`, `Permissions` (nvarchar(2048), comma-separated permission keys), `CreatedAt`. Index `IX_Roles_OrganizationId`.
4. `OrganizationInvites` — PK `Id`. FK `OrganizationId`. `Email`, `Role`, `CustomRoleId?`, `Token` (unique), `ExpiresAt`, `AcceptedAt?`, `InvitedBy`. Index `IX_OrganizationInvites_OrganizationId`.
5. `ProjectClients` — PK `Id`. FK `ProjectId` → `Projects.Id` (cascade), FK `UserId` → `Users.Id`, `OrganizationId`. `AccessLevel` (default `ProgressOnly`), `AddedAt`, `RemovedAt?`. Unique `UX_ProjectClients_Project_User`.
6. `ClientFeedback` — PK `Id`. FK `ProjectId`, `AuthorUserId`. `Body` (nvarchar(max)), `Kind` (`ClientFeedbackKind`), `Status` (`ClientFeedbackStatus`), `PmResponse?`, `ResolvedBy?`, `CreatedAt`. Index `IX_ClientFeedback_ProjectId_CreatedAt`.
7. `ProjectHandoffs` — PK `Id`. FK `ProjectId` (one-to-one). `Status` (`HandoffStatus`), `ChecklistJson`, `InitiatedBy`, `AcceptedBy?`, `CompletedAt?`, `CreatedAt`. Index `UX_ProjectHandoffs_ProjectId` (unique).
8. `HandoffDocuments` — PK `Id`. FK `HandoffId` → `ProjectHandoffs.Id` (cascade). `Title`, `BlobKey?` (populated by backend 11), `Kind` (`HandoffDocumentKind`), `GeneratedByAi`, `UploadedBy`, `CreatedAt`. Index `IX_HandoffDocuments_HandoffId`.
9. `OrganizationLifecycleEvents` — PK `Id`. FK `OrganizationId` (cascade). `Kind` (`LifecycleEventKind`), `ActorUserId`, `PayloadJson`, `CreatedAt`. Index `IX_OrganizationLifecycleEvents_OrgId_CreatedAt`.

## Changed entities (tenant column + enums)

- `Workspaces`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `Invites`, `Notifications` gain **`OrganizationId`** (uuid, NOT NULL; backfilled from ownership chain in migration `AddMultiTenantColumns`; boards/columns store their project's org id). Index `IX_{table}_OrganizationId` on each. `Attachments.BlobKey` stays pending backend 11.
- `ApiLogs`, `ErrorLogs`, `AuditLogs`, `ActivityLogs` gain **nullable `OrganizationId`** (null = platform-level event) + index.
- `Users` gains `PlatformRole` (`PlatformRole` enum: `User`, `SuperAdmin`; default `User`).
- New enums: `OrganizationStatus` (`Active`, `Suspended`, `Offboarding`, `Archived`), `OrganizationPlan` (`Free`, `Pro`, `Enterprise`), `OrganizationRole` (`Owner`, `Admin`, `ProjectManager`, `Member`, `Client`, `Custom`), `PlatformRole`, `ClientFeedbackKind` (`Comment`, `Suggestion`, `EditRequest`), `ClientFeedbackStatus` (`New`, `Acknowledged`, `Resolved`, `Rejected`), `HandoffStatus` (`NotStarted`, `InProgress`, `AwaitingClientAcceptance`, `Completed`), `HandoffDocumentKind` (`Manual`, `Credential`, `Design`, `Report`, `Other`), `LifecycleEventKind` (`Onboarded`, `Suspended`, `Reactivated`, `OffboardStarted`, `DataExported`, `Offboarded`, `Purged`).
- `ProjectStatus` enum gains values: `Handoff`, `Maintenance`, `PostDeploymentSupport` (append-only; existing ordinals unchanged).

## Relationships added (cardinality)

- `Organizations 1—N OrganizationMembers`, `1—N Roles`, `1—N OrganizationInvites`, `1—N OrganizationLifecycleEvents`, `1—N Workspaces`
- `Users 1—N OrganizationMembers` (a user joins many companies), `1—N ProjectClients`
- `Projects 1—N ProjectClients`, `1—1 ProjectHandoffs`, `1—N ClientFeedback`
- `ProjectHandoffs 1—N HandoffDocuments`

## Isolation contract

Every tenant-owned entity gets an EF Core global query filter on `OrganizationId` + the repository tenant guard; SQL Server has no RLS, so the boundary is `ITenantContext` (JWT `org` claim) → global filters → write-assert → integration tests (qa 14). Full rationale: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §1.
