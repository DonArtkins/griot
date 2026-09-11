# ERD Amendment v2 — Multi-Tenancy (✅ Approved by operator 2026-09-11)

**Status:** ✅ APPROVED + EXPORTED (operator approval recorded 2026-09-11 during spec-29
implementation and confirmed on the v2 canvas; versioned PNGs
`diagrams/erd/griot-erd-v2.0.0.png` + `diagrams/erd/griot-erd2-v2.0.0.png` +
`diagrams/erd/griot-erd3-v2.0.0.png` added 2026-09-11, together with the v2
architecture PNGs `diagrams/architecture/c4-system-context-v2.0.0.png` +
`diagrams/architecture/griot-api-v2.0.0.png` — full set indexed in the
`diagrams/README.md` ledger). Per hard rule 4 (ERD before schema),
the spec-29 migration implements exactly this contract.
**FK correction (2026-09-11, shipped):** all Organization FKs are **ON DELETE NO ACTION**
(RESTRICT) — the cascade variant failed with SQL error 1785 (multiple cascade paths via
the owner edges and the Workspace→Project chain). Org removal is app-managed (spec-33
offboarding purge); the lines below reflect this correction and the v2 PNGs show the
org→child edges without cascade notation.
**Owner:** backend spec 29 (`feature-specs/29-multi-tenant-foundation-organizations.md`). Canonical field list: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §2 + §9 (account-deletion delta).
**Precedent:** this follows the same amendment pattern as `diagrams/erd/auth-family-amendment.md` (RefreshTokens family columns), which was approved without re-opening the full v1.0.0 ERD.

## New entities (10 — entity 10 is the §9 account-deletion delta)

1. `Organizations` — tenant root. PK `Id` (uuid). `Name`, `Slug` (unique, `UX_Organizations_Slug`), `OwnerId` → `Users.Id`, `Status` (`OrganizationStatus`), `Plan` (`OrganizationPlan`), `CreatedAt`, `UpdatedAt`, `OffboardedAt?`. Index: `IX_Organizations_OwnerId`.
2. `OrganizationMembers` — PK `Id`. FK `OrganizationId` → `Organizations.Id` (NO ACTION), FK `UserId` → `Users.Id`. `Role` (`OrganizationRole`), `CustomRoleId?` → `Roles.Id` (nullable; only valid when `Role == Custom`), `Status` (member status: `Invited`/`Active`/`Suspended`), `JoinedAt`. Unique index `UX_OrganizationMembers_Org_User (OrganizationId, UserId)`; index `IX_OrganizationMembers_UserId`.
3. `Roles` — PK `Id`. FK `OrganizationId` → `Organizations.Id` (NO ACTION). `Name`, `IsSystem`, `Permissions` (nvarchar(2048), comma-separated permission keys), `CreatedAt`. Index `IX_Roles_OrganizationId`.
4. `OrganizationInvites` — PK `Id`. FK `OrganizationId`. `Email`, `Role`, `CustomRoleId?`, `Token` (unique), `ExpiresAt`, `AcceptedAt?`, `InvitedBy`. Index `IX_OrganizationInvites_OrganizationId`.
5. `ProjectClients` — PK `Id`. FK `ProjectId` → `Projects.Id` (cascade), FK `UserId` → `Users.Id`, `OrganizationId`. `AccessLevel` (default `ProgressOnly`), `AddedAt`, `RemovedAt?`. Unique `UX_ProjectClients_Project_User`.
6. `ClientFeedback` — PK `Id`. FK `ProjectId`, `AuthorUserId`. `Body` (nvarchar(max)), `Kind` (`ClientFeedbackKind`), `Status` (`ClientFeedbackStatus`), `PmResponse?`, `ResolvedBy?`, `CreatedAt`. Index `IX_ClientFeedback_ProjectId_CreatedAt`.
7. `ProjectHandoffs` — PK `Id`. FK `ProjectId` (one-to-one). `Status` (`HandoffStatus`), `ChecklistJson`, `InitiatedBy`, `AcceptedBy?`, `CompletedAt?`, `CreatedAt`. Index `UX_ProjectHandoffs_ProjectId` (unique).
8. `HandoffDocuments` — PK `Id`. FK `HandoffId` → `ProjectHandoffs.Id` (cascade). `Title`, `BlobKey?` (populated by backend 11), `Kind` (`HandoffDocumentKind`), `GeneratedByAi`, `UploadedBy`, `CreatedAt`. Index `IX_HandoffDocuments_HandoffId`.
9. `OrganizationLifecycleEvents` — PK `Id`. FK `OrganizationId` (NO ACTION). `Kind` (`LifecycleEventKind`), `ActorUserId`, `PayloadJson`, `CreatedAt`. Index `IX_OrganizationLifecycleEvents_OrgId_CreatedAt`.

10. `AccountDeletionRequests` (guide §9 delta — PLANNED) — PK `Id`. FK `UserId` → `Users.Id`. `Reason` (nvarchar(2000), required), `Status` (`DeletionRequestStatus`: `Requested`, `Approved`, `Rejected`, `Cancelled`, `ExportAvailable`, `PurgeScheduled`, `Purged`), `RequestedAt`, `DecidedBy?` FK `Users.Id`, `DecidedAt?`, `DecisionNote?` (nvarchar(2000)), `ExportBlobKey?` (backend 11), `ScheduledPurgeAtUtc?` (NOT NULL once set), `PurgedAt?`, `CancelReason?` (nvarchar(2000)). Index `IX_AccountDeletionRequests_UserId`; **partial unique index** (one open request per user — enforced at DB level, also asserted by app before insert):
    ```sql
    CREATE UNIQUE INDEX UX_AccountDeletionRequests_OneOpenPerUser
    ON AccountDeletionRequests(UserId)
    WHERE Status IN ('Requested','Approved','ExportAvailable','PurgeScheduled');
    ```

## Changed entities (tenant column + enums)

- `Workspaces`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `Invites`, `Notifications` gain **`OrganizationId`** (uuid, NOT NULL; backfilled from ownership chain in migration `AddMultiTenantColumns`; boards/columns store their project's org id). Index `IX_{table}_OrganizationId` on each. `Attachments.BlobKey` stays pending backend 11.
- `ApiLogs`, `ErrorLogs`, `AuditLogs`, `ActivityLogs` gain **nullable `OrganizationId`** (null = platform-level event) + index.
- `Users` gains `PlatformRole` (`PlatformRole` enum: `User`, `SuperAdmin`; default `User`) + `DeletionRequestId?` → FK `AccountDeletionRequests.Id` (nullable; `ON DELETE SET NULL` when `AccountDeletionRequests` rows are pruned after the 365-day tail). Read-mostly flag: when non-null the account is in DeletionPending state (§9f). `Users.DeletedAt` still set ONLY by the purge job (guide §9g last step) — NEVER set by the request route directly.
- New enums: `OrganizationStatus` (`Active`, `Suspended`, `Offboarding`, `Archived`), `OrganizationPlan` (`Free`, `Pro`, `Enterprise`), `OrganizationRole` (`Owner`, `Admin`, `ProjectManager`, `Member`, `Client`, `Custom`), `PlatformRole` (`User`, `SuperAdmin`), `ClientFeedbackKind` (`Comment`, `Suggestion`, `EditRequest`), `ClientFeedbackStatus` (`New`, `Acknowledged`, `Resolved`, `Rejected`), `HandoffStatus` (`NotStarted`, `InProgress`, `AwaitingClientAcceptance`, `Completed`), `HandoffDocumentKind` (`Manual`, `Credential`, `Design`, `Report`, `Other`), `LifecycleEventKind` (14 values total, append-only): 7 org-lifecycle (`Onboarded`, `Suspended`, `Reactivated`, `OffboardStarted`, `DataExported`, `Offboarded`, `Purged`) + **7 user-lifecycle (`AccountDeletionRequested`, `AccountDeletionApproved`, `AccountDeletionRejected`, `AccountDeletionExported`, `AccountDeletionPurgeScheduled`, `AccountDeletionPurgeCancelled`, `AccountDeletionPurged`)**, `DeletionRequestStatus` (`Requested`, `Approved`, `Rejected`, `Cancelled`, `ExportAvailable`, `PurgeScheduled`, `Purged` — append-only). **Existing enum `NotificationType` gains `AccountDeletion`** (spec 22 fan-out for co-user blast-radius notices at approval time).
- `ProjectStatus` enum gains values: `Handoff`, `Maintenance`, `PostDeploymentSupport` (append-only; existing ordinals unchanged).

## Relationships added (cardinality + FK behavior)

- `Organizations 1—N OrganizationMembers`, `1—N Roles`, `1—N OrganizationInvites`, `1—N OrganizationLifecycleEvents`, `1—N Workspaces` (OrganizationId FK, **ON DELETE NO ACTION** — SQL Server forbids cascading from Organizations to both Workspaces and Projects (multiple cascade paths, error 1785); org removal is app-managed: the spec-33 offboarding purge deletes children explicitly in dependency order inside one transaction; org suspension = writes 403, no delete)
- `Users 1—N OrganizationMembers` (a user joins many companies), `1—N ProjectClients`, `1—N AccountDeletionRequests` (guide §9; at most one **open** per user — partial unique index enforces this; multiple historical cancelled/purged rows allowed per user). **Reverse FK:** `Users.DeletionRequestId? → AccountDeletionRequests.Id` (nullable; ON DELETE SET NULL when AccountDeletionRequests rows are pruned).
- `Projects 1—N ProjectClients`, `1—1 ProjectHandoffs` (unique UX constraint), `1—N ClientFeedback`
- `ProjectHandoffs 1—N HandoffDocuments`
- `AccountDeletionRequests N—1 Users` (UserId FK; cascade on Users hard-delete but Users are soft-deleted so cascade never fires in practice)

## Isolation contract

Every tenant-owned entity gets an EF Core global query filter on `OrganizationId` + the repository tenant guard; SQL Server has no RLS, so the boundary is `ITenantContext` (JWT `org` claim) → global filters → write-assert → integration tests (qa 14). Full rationale: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §1.
