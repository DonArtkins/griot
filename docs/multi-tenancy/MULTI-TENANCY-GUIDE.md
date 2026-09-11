# Griot Multi-Tenancy Guide — Companies, Lifecycle, Roles, Client Support, Critical-Action OTP & Safe Deletion [own-stack]

**Created:** 2026-09-11 · **Last updated:** 2026-09-11 (OTP + account-deletion-request + destructive-safety wave)
**Status:** SPEC 29 IMPLEMENTED (2026-09-11, commit `9447e15` on `feature/backend/29-multi-tenant-foundation-organizations` — schema + Pool-model isolation live; organization lifecycle/read surface ships with specs 30–35). Contract sections below remain authoritative; the spec-29 ERD amendment (`diagrams/erd/multi-tenant-amendment.md`) was APPROVED + EXPORTED 2026-09-11 (v2 PNGs in the `diagrams/README.md` ledger) and spec 29 is marked complete.
**Source of research:** `research/LYNCXS-MULTI-TENANT-SYSTEMS-ENGINEERING.md` (v1.0.0) + `research/ai-features-research.md` §1/§3.6 (OTP/step-up) + `research/ai-integration.md` §2a (AI boundary)
**Owner:** backend spec 29 (foundation) — dependents: backend 30–51, all other systems' bumped specs
**Contract-sync:** every cross-system surface in this file is mirrored in `project-kit/context/integration-contracts.md`, `docs/api/auth-contract.md`, `docs/communication/COMMUNICATION-GUIDE.md`, `docs/observability/HOW-LOGGING-WORKS.md`, `diagrams/erd/multi-tenant-amendment.md`, and each owning spec (23/21/22/32/33 + web 13/14 + mobile 08/09 + qa 14). Planned behavior is labeled PLANNED and is not acceptance evidence until its owning spec ships.

This document is the single canonical contract for converting Griot from a single-tenant project-management app into a **multi-tenant platform**: the operator (you) onboards **Companies (Organizations)** as tenants, each company gets an **Admin (owner of the company)**, and the rest of the existing hierarchy (workspaces → projects → boards → tasks) hangs *below* the company. Everything here is **PLANNED** — it becomes implemented acceptance evidence only when its owning spec ships on its own feature branch.

## 1. Tenancy model — Pool (research §2, §12.1)

**Decision (ADR-005 to be written at implementation):** **Pool model** — one SQL Server 2022 database, one schema, every tenant-owned row carries `OrganizationId`. Per the research volume, Pool is the correct default for standard B2B SaaS with many small-to-mid tenants. Bridge/Silo (schema-per-tenant / database-per-tenant) and Hybrid promotion are explicitly **deferred**; revisit only if an enterprise contract demands physical isolation.

- SQL Server has no Postgres RLS. The isolation boundary is therefore **three layers that survive an application bug**:
  1. `ITenantContext` resolved per request by `TenantResolutionMiddleware` (from the JWT `org` claim — never from a caller-supplied header/body) and exposed via `IHttpContextAccessor`.
  2. **EF Core global query filters** (`HasQueryFilter(e => e.OrganizationId == tenantContext.OrganizationId)`) on every tenant-owned entity, so even a forgotten `Where` cannot cross tenants; writes assert `OrganizationId` matches the context or throw.
  3. **Repository guard** in `IGenericRepository` save paths + integration tests that prove cross-tenant reads return empty and writes are rejected (qa spec 14).
- Cache keys, rate-limit partitions, notification fan-out jobs and outbox rows all carry the tenant id (`cache:org:{orgId}:…`, `ratelimit:org:{orgId}:…`) — see backend 19/22 bumps.
- `ApiLogs`/`ErrorLogs`/`AuditLogs`/`ActivityLogs` gain `OrganizationId` (nullable for platform-level/system events) so per-tenant log reads are possible (feeds backend 25 role-tiered log access and mcp 07).

## 2. Canonical domain model (ERD amendment v2 — PLANNED, Figma Make approval required before schema code)

New/changed entities (names are contract once the ERD amendment is approved — full column/index list: `diagrams/erd/multi-tenant-amendment.md`):

| Entity | Purpose | Key fields |
|---|---|---|
| `Organizations` | The tenant (a Company) | `Id`, `Name`, `Slug` (unique), `OwnerId` (the company Admin), `Status` (`OrganizationStatus`: `Active`, `Suspended`, `Offboarding`, `Archived`), `Plan` (`OrganizationPlan`: `Free`, `Pro`, `Enterprise` — display/billing metadata only, no payments this wave), `CreatedAt`, `UpdatedAt`, `OffboardedAt?` |
| `OrganizationMembers` | A user's membership in a company | `OrganizationId`, `UserId`, `Role` (`OrganizationRole`), `CustomRoleId?` (FK `Roles`, only when `Role == Custom`), `Status` (`Invited`/`Active`/`Suspended`), `JoinedAt` |
| `Roles` | Custom roles created **inside one company** by its Admin/PM | `OrganizationId`, `Name`, `IsSystem` (bool), `Permissions` (comma-separated permission keys) |
| `OrganizationInvites` | Company-scoped invite chain (workspace invites remain) | `OrganizationId`, `Email`, `Role`, `CustomRoleId?`, `Token`, `ExpiresAt`, `AcceptedAt?` |
| `ProjectClients` | A client (user w/ `Client` role) attached to a project | `ProjectId`, `UserId`, `OrganizationId`, `AccessLevel` (`ProgressOnly` default), `AddedAt`, `RemovedAt?` |
| `ClientFeedback` | Client comments/suggestions routed to the PM | `ProjectId`, `AuthorUserId`, `Body`, `Kind` (`Comment`/`Suggestion`/`EditRequest`), `Status` (`New`/`Acknowledged`/`Resolved`/`Rejected`), `PmResponse?`, `CreatedAt` |
| `ProjectHandoffs` | The handoff record per project | `ProjectId`, `Status` (`NotStarted`/`InProgress`/`AwaitingClientAcceptance`/`Completed`), `ChecklistJson`, `InitiatedBy`, `CompletedAt?` |
| `HandoffDocuments` | Files/artifacts handed to the client | `HandoffId`, `Title`, `BlobKey` (backend 11), `Kind` (`Manual`/`Credential`/`Design`/`Report`/`Other`), `GeneratedByAi` (bool), `UploadedBy`, `CreatedAt` |
| `OrganizationLifecycleEvents` | Onboarding/offboarding/suspension audit trail | `OrganizationId`, `Kind` (`Onboarded`/`Suspended`/`Reactivated`/`OffboardStarted`/`DataExported`/`Offboarded`/`Purged` — plus user-lifecycle values `AccountDeletionRequested`/`AccountDeletionApproved`/`AccountDeletionRejected`/`AccountDeletionExported`/`AccountDeletionPurgeScheduled`/`AccountDeletionPurgeCancelled`/`AccountDeletionPurged`), `ActorUserId`, `PayloadJson`, `CreatedAt` |
| `AccountDeletionRequests` (§9) | User-initiated delete request → SuperAdmin review → export → 30-day purge | `Id`, `UserId` FK `Users` + `IX_AccountDeletionRequests_UserId` (partial unique: one open request per user), `Reason` nvarchar(2000) required, `Status` = `DeletionRequestStatus`: `Requested`, `Approved`, `Rejected`, `Cancelled`, `ExportAvailable`, `PurgeScheduled`, `Purged` (append-only), `RequestedAt`, `DecidedBy?`, `DecidedAt?`, `DecisionNote?`, `ExportBlobKey?` (backend 11), `ScheduledPurgeAtUtc?`, `PurgedAt?`, `CancelReason?` |

Enums: `OrganizationStatus`, `OrganizationPlan`, `OrganizationRole` (`Owner`, `Admin`, `ProjectManager`, `Member`, `Client`, `Custom`), `PlatformRole` (`User`, `SuperAdmin`), `ClientFeedbackKind`, `ClientFeedbackStatus`, `HandoffStatus`, `HandoffDocumentKind`, `LifecycleEventKind`, `DeletionRequestStatus`. `NotificationType` gains `AccountDeletion` (spec 22 fan-out). `ProjectStatus` gains `Handoff`, `Maintenance`, `PostDeploymentSupport` values (append-only; existing ordinals unchanged).

**Tenant column rollout:** `Workspaces`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `Invites`, `Notifications`, and the four observability tables all gain `OrganizationId` (boards/columns store their project's org id). `Users` and `RefreshTokens` stay **global** (a user may belong to many companies; sessions survive org switching).


## 3. Role model — layered RBAC (research §6.1–6.5)

Three layers, resolved in precedence order (highest wins):

1. **Platform role** — `Users.PlatformRole` (`User` default, `SuperAdmin`). SuperAdmin is **the operator (owner of Griot itself)**: manages all companies, sees the platform console, is the only actor who can onboard/suspend/offboard companies. Bootstrapped idempotently from `SUPERADMIN__EMAIL` env on startup.
2. **Organization role** — `OrganizationMembers.Role`. System-seeded per company: **`Owner`/`Admin` (the company owner — can view ALL projects under that company only, manage members, create custom roles), `ProjectManager` (manages only their assigned projects), `Member`**. Admin/PM can **create additional custom roles** in their own company (`Roles`; `Role == Custom` + `CustomRoleId`), composing only the fixed permission catalogue (no platform or lifecycle powers, ever).
3. **Project/client scope** — a ProjectManager's authority is bounded to projects where they are assigned; **`Client`** org role sees only `ProjectClients`-attached projects in the client-portal view (read-only progress + feedback), enforced server-side and mirrored by the AI capability gateway (backend 25 bump, ai 13).

**Permission catalogue (fixed keys, used by the `perms` JWT claim):** `org.read`, `org.settings.manage`, `org.members.manage`, `org.roles.manage`, `org.projects.view_all`, `project.manage`, `task.manage`, `comment.write`, `client.manage`, `client.feedback.read`, `client.feedback.respond`, `report.generate`, `log.read_tier` (tier rules owned by backend 25). Custom roles compose these keys only.

| Actor | Sees |
|---|---|
| SuperAdmin (platform) | everything — all companies, platform console, lifecycle |
| Company Admin (`Owner`/`Admin`) | ALL projects inside their company only |
| ProjectManager | only their assigned projects (full manage) |
| Member | workspaces/projects they are members of |
| Client | only their attached projects' progress view + feedback |

## 4. JWT v2 — token contract (owner: backend spec 30)

- **Access token (JWT, HS256, 15 min — lifetimes unchanged):** adds claims to the existing `sub`, `email`, `jti`, `iss=Griot`, `aud=GriotClients`:
  - `name` — `Users.DisplayName`
  - `org` — the **active** `OrganizationId` (selected via `POST /api/auth/select-organization`; re-issued on switch). Absent only for platform-only sessions.
  - `role` — effective role in the active org (`super_admin` when `PlatformRole == SuperAdmin`, else the `OrganizationRole`; `custom:{roleId}` for custom roles)
  - `perms` — space-separated permission keys for the active role (empty for pure `Member`)
- **Refresh token stays opaque — deliberately.** 64-hex random bytes, SHA-256 at rest, rotation + family revoke (unchanged). It is *not* a JWT and must never become one: putting user data into a long-lived token violates the research volume §5 (opaque rotating refresh tokens) and OWASP guidance; jwt.io showing it blank is **correct behavior**, not a defect. Verification: the access token decodes **and verifies** at jwt.io when the real `Jwt:Key` is pasted — `a-string-secret-at-least-256-bits-long` is jwt.io's *placeholder*, which is why pasting it fails verification.
- **Signing key policy:** `JWT__Key` must be ≥ 64 chars (512 bits) in production, generated via a CSPRNG, stored in the platform secret store (Railway env), never committed (`appsettings.Local.json` is git-ignored); rotation runbook documented in spec 30. Dev keys already exceed 256 bits.
- SuperAdmin bootstrap, org-switch endpoint and the full route table live in `docs/api/auth-contract.md` (PLANNED section added in this wave).

## 5. Company lifecycle — onboarding → manage → offboard (owners: backend 32/33)

**Onboard (SuperAdmin):** `POST /api/organizations` (SuperAdmin; self-serve signup is a later option) → creates `Organizations` row + owner user (invite-by-email flow reusing Brevo branded templates) + seeds the system roles + a default workspace + `OrganizationLifecycleEvents(Onboarded)`. Bulk provisioning (SCIM 2.0) is deferred per research §12.4 until an enterprise customer demands it.

**Manage:** suspend (`POST /api/organizations/{id}/suspend` — auth kept, writes rejected with 403 `org_suspended`), reactivate, ownership transfer, plan/metadata edits. The Company Admin manages everything *inside* their company only.

**Offboard (SuperAdmin):** `POST /api/organizations/{id}/offboard` → 1) export bundle (JSON of all org data; artifacts via blob backend 11) → 2) `Status = Offboarding`, 30-day retention window (`Organizations:RetentionDays`, default 30) → 3) purge/anonymize (`Purged` lifecycle event; personal data removed, legal/financial records retained per research §8.3). Org FKs are all ON DELETE NO ACTION (Restrict), so nothing is deleted by FK cascade at the org level: the app-managed purge deletes leaves-first — workspace/project subtrees (boards → columns → tasks → comments/attachments), then `OrganizationMembers`, `Roles`, `OrganizationInvites`, `OrganizationLifecycleEvents` — before deleting the `Organizations` row (per-chunk transactions with `OrganizationLifecycleEvents` progress checkpoints; specs 33 + 41). Every step is an audit event; SuperAdmin notifications use backend 27 broadcasts.

## 6. Client support surface (owners: backend 34, web 15, ai 13)

The most important product axis: **client satisfaction**. A project's client is onboarded (`ProjectClients`, `Client` role, scoped invite), sees a **different progress view** (percent-complete, milestones, recent activity digest — no internal board internals beyond the granted level), can **comment and suggest edits** (`ClientFeedback` → routed to the assigned ProjectManager via notification + AI-assisted triage summary), and the AI (ai 13) answers client questions **only** from client-scoped data (capability gateway extension of backend 25).

## 7. Handoff, client offboarding, maintenance phase (owners: backend 35, web 16, ai 14)

Project close-out: `ProjectHandoffs` checklist (deliverables, credentials, environments, docs) → **AI generates the client user manual** into `HandoffDocuments` (`Kind = Manual`, `GeneratedByAi = true`) → client accepts → **client offboarding** (access narrowed to portal-read-only + maintenance requests) → project moves to `Maintenance`/`PostDeploymentSupport` where the client can raise support requests and the PM (or AI triage) responds. **Client data is retained** after offboarding so maintenance stays possible; only full *company* offboarding purges.

## 8. Critical-action OTP — required for every important operation (owner: backend 23) — PLANNED

Registration already proves the pattern (implemented, spec 07): `email_verify` OTP is auto-sent on register and `Users.EmailVerified` gates trust. This wave extends the same Brevo Email-only OTP mechanism (single verified sender `Brevo:FromEmail`, best-effort except `otp/request` which surfaces 502) to every important operation, with emphasis on auth actions. No TOTP/passkey this wave (enum reserves `Totp`).

**OTP purpose enumeration — exhaustive:**

| Purpose | Where used | Action param |
|---|---|---|
| `email_verify` | Register (auto-sent, spec 07 ✅) | — |
| `login_2fa` | Login (enforced per user policy, see below) | — |
| `password_reset` | Forgot password + Reset password | — |
| `delete_account` | Account deletion request submit (§9) | — |
| `step_up` | Curated guarded operations (last column below) | Required — one of the `Security:StepUpActions` allow-list keys |

**Policy: OTP is required for every listed operation.** A guarded route called without the matching OTP challenge/step-up claim returns 403. There is no "skip OTP" override.

**Login OTP policy (default for v1):** OTP is enforced when `Users.TwoFactorMethod = EmailOtp` (opt-in per account; set to `EmailOtp` by default for SuperAdmin and Company Admin accounts for defence-in-depth). All other accounts may opt in via settings. The `POST /api/auth/login` response distinguishes: 200 with tokens when `TwoFactorMethod = None`; 202 `{twoFactorRequired:true, purpose:"login_2fa"}` (no tokens) when `EmailOtp`. *Future wave option:* a platform-wide `Security:RequireLoginOtpForAll = true` flag flips the default so every login is OTP-challenged regardless of `TwoFactorMethod`.

**Full OTP operation matrix:**

| Operation | OTP purpose | Gate |
|---|---|---|
| Register (done) | `email_verify` | auto-sent on `POST /api/auth/register` (spec 07 ✅) |
| Login (enforced when `Users.TwoFactorMethod = EmailOtp`; opt-in for others) | `login_2fa` | `POST /api/auth/login` → 202 `{twoFactorRequired:true}` without tokens → `otp/verify(login_2fa)` issues the pair |
| Forgot password | `password_reset` | `POST /api/auth/forgot-password` → uniform 202 even for unknown email (no enumeration) |
| Reset password | `password_reset` | `POST /api/auth/reset-password {email, code, newPassword}` → Argon2 re-hash + revoke all refresh families |
| Account deletion request (§9, never instant delete) | `delete_account` | `otp/request(delete_account)` → verify → 5-min `stepUpAccessToken` → `POST /api/auth/account/deletion-request {reason, stepUpAccessToken}` |
| Account export download (§9) | `step_up` (`action=Account.ExportData`) | `RequireStepUp("Account.ExportData")` on the download route; short-lived blob link (backend 11) + step-up at click |
| Org lifecycle destroys | `step_up` | `Organization.TransferOwnership`, `Organization.OffboardStart` (SuperAdmin) + company-Admin destroy actions; claim binds `org` (org A claim never authorizes org B) |
| Curated guarded ops (`Security:StepUpActions` allow-list) | `step_up` | `RequireStepUp(action)` — 403 without claim, 409 on action mismatch. Allow-list default keys: `Workspace.Delete`, `Workspace.TransferOwnership`, `Member.RoleChange`, `Member.Remove`, `Invite.Create`, `Project.Delete`, `Board.Delete`, `Column.Delete`, `Task.BulkStatus`, `Task.Delete`, `Comment.Delete`, `Attachment.Delete`, `Account.Delete`, `Account.ExportData`, `AccountDeletion.Decide`, `Organization.TransferOwnership`, `Organization.OffboardStart` |

**Non-negotiable OTP security rules (from spec 23, cross-referenced for clarity):**
6-digit codes from `RandomNumberGenerator`; stored as HMAC-SHA256 with `Otp:Pepper` in `OtpChallenges.CodeHash`; **never logged, never returned in any response body or header**; verification uses constant-time compare (CryptographicOperations.FixedTimeEquals — no early-exit string compare); 10-minute expiry (`OtpChallenges.ExpiresAt`); 5-attempt lockout per `OtpChallenges.AttemptCount` (429 `{error: "otp_lockout", retryAfterSec}`). Redis `ratelimit:otp:request:{email}` 3 requests / 15 min per email + API global limiter 100/min/caller; 429 carries `Retry-After` header. Every request/verify/failed-attempt/lockout/issued-step-up writes an `AuditLogs` row (spec 20 pipeline, org-stamped per spec 51). AI OBO callers are **permanently 403** on this entire surface via `RejectAiOnAuthSurface()` (runs before rate limiter and any service call; xUnit asserts 403 per route). Branded Brevo template per purpose; single dashboard-verified sender (spec 12).

## 9. Account deletion — request to SuperAdmin, reason required, export on approval, purge 30 days after request — PLANNED

**🚨 SUPERSESSION — hard rule, no exceptions.** The v1 `DELETE /api/auth/account` route (instant soft-delete: immediate 204 → `Users.DeletedAt` set → revoke refresh tokens) is **REMOVED from code** on the spec-23 branch. Any caller of the old route receives **HTTP 410 Gone**. The ONLY account-deletion path is the request workflow below. Spec 23 records this supersession in its contract-changes banner and xUnit asserts 410 on the old DELETE route.

States (append-only, every transition → AuditLogs + user-lifecycle LifecycleEvent row): `None → DeletionRequested → (SuperAdmin Approved \| Rejected \| UserCancelled) → (Approved: ExportAvailable → PurgeScheduled → Purged)`. Requester may cancel while `DeletionRequested` (any time before SuperAdmin decides); SuperAdmin may cancel while `PurgeScheduled` (inside the 30-day window) → back to `Active` with full audit. Cancels are permanent and cannot be "un-cancelled"; a cancelled user may submit a new request with a new 30-day window.

### 9a. Request flow (user)

Settings → Delete account → **Consequence screen first** (before any OTP): "This deletes the ENTIRE CHAIN of data you own. No orphans: workspaces, projects, boards, columns, tasks, comments, attachments, invites, notifications, and memberships everywhere you belong. Shared resources you own will be re-homed to other members OR permanently deleted if no one else is in them — and ALL users sharing those resources will be notified with a 30-day save window. This cannot be undone once purge runs." → **Mandatory free-text reason** (10–2000 characters; 400 `{error:"reason_required_or_invalid"}` when blank/too-short/too-long; stored both on `AccountDeletionRequests.Reason` and the `AuditLogs.PayloadJson`) → `otp/request(delete_account)` → user enters 6-digit code → `otp/verify(delete_account)` → 5-min `stepUpAccessToken` (claims: `stepup=true, purpose=delete_account, challengeId={OtpChallenges.Id}`) → `POST /api/auth/account/deletion-request {reason, stepUpAccessToken}` → **202** `{requestId, status:"DeletionRequested", requestedAtUtc, scheduledPurgeAtUtc}`. Login still works after request; a platform-wide banner shows "Account deletion requested on {date}. It will be permanently deleted on {scheduledPurgeAtUtc}. Cancel in Settings before then if you change your mind." with a one-click cancel.

### 9b. 30-day purge window formula — SINGLE SOURCE OF TRUTH

Per your requirement: **"30 DAYS AFTER THE REQUEST WAS SENT"** (NOT approval date). Formula with safety guard:

```
scheduledPurgeAtUtc = requestedAtUtc + Deletion.PurgeWindowDays  (default 30)
if SuperAdmin approves late and (scheduledPurgeAtUtc - decisionAtUtc) < Deletion.MinPurgeWindowDays:
   scheduledPurgeAtUtc = decisionAtUtc + Deletion.MinPurgeWindowDays   (default 7, never < 7, never > 90 even with override)
```

Configuration (single source: `project-kit/context/integration-contracts.md` env table): `Deletion:PurgeWindowDays` (default 30), `Deletion:MinPurgeWindowDays` (default 7, floor=7, ceiling=90). Example: request sent on Day 0 → `scheduledPurgeAtUtc = Day 30`. If SuperAdmin approves on Day 29 → remaining window = 1 day → clamped to Day 29 + 7 = Day 36. If SuperAdmin approves on Day 5 → window stays Day 30. User sees the exact `scheduledPurgeAtUtc` in Settings, in the request banner, in the approval email, and in every co-user notice.

### 9c. Review flow (SuperAdmin)

New request notifies SuperAdmin via the spec-27 confirmed-notice path (draft → confirm → send) + in-app `System` notification + Brevo email. `GET /api/admin/account-deletion-requests` (SuperAdmin only, paginated per spec 18, filterable by status, sorted by `RequestedAt` desc). `POST /api/admin/account-deletion-requests/{id}/{approve,reject}` (both step-up gated: `RequireStepUp("AccountDeletion.Decide")`).
- **Reject →** `Status = Rejected`, user gets an email + in-app notification ("Your account deletion request was reviewed and rejected. Reason: {note}. Your account remains active."), audit row with `DecisionNote`; request row retained for 365 days then pruned (spec 20). User may submit a new request after 7 days (cooling-off).
- **Approve →** triggers §9d (export build), §9e (blast-radius fan-out), §9f (read-mostly lock), `Status = ExportAvailable` → immediately followed by `PurgeScheduled` with the §9b date. Export is built first (§9d) and fan-out email fires only when bundle exists (no false promises of download).

### 9d. Export on approval — immediate, re-issuable, FULL data bundle

Within 1 hour of approval the service builds the user data bundle following spec-33 export manifest + checksum discipline:
- One JSON file per entity family the user owns OR touches: `users/self.json` (sanitized — no PasswordHash, no token hashes), `workspaces/owned.json`, `workspaces/membership.json`, `projects/owned.json`, `projects/membership.json`, `boards.json`, `columns.json`, `tasks/assigned.json`, `tasks/authored.json`, `comments.json`, `attachments/metadata.json` (blob URLs become short-lived signed links valid 24h from download), `invites/sent.json`, `notifications.json`, `memberships.json`, `client-feedback.json`, `handoffs.json`.
- `manifest.json` (entity counts, sha256 per file, generatedAt, userId).
- Bundled as a zip, stored in backend-11 blob storage under `ExportBlobKey`, 90-day TTL.

User gets **both**:
1. Brevo email with short-lived download link (24h, regenerable) + "After clicking, you will be prompted to re-verify your identity with an OTP (`Account.ExportData` step-up)."
2. In-app notification with same link.

Re-issuable: `GET /api/auth/account/export` (bearer + `RequireStepUp("Account.ExportData")`, only while status ∈ {ExportAvailable, PurgeScheduled}) **streams the existing bundle from blob AND rebuilds it in the background every 7 days** so the export reflects latest state (e.g., if another user adds a comment on a shared task before purge, the re-download includes that diff). Each issuance + each download is an AuditLogs row. Export is re-issuable for the entire window up to purge.

### 9e. Blast-radius fan-out — co-users notified with save/export instructions

At approval (same transaction that sets `Status = PurgeScheduled`), the service computes the **blast radius** by walking the ownership + membership graph:

Blast radius = every user who has membership (WorkspaceMember, ProjectClient, OrganizationMember) in a Workspace/Project/Board owned by the requester OR where the requester was the sole remaining Admin/Owner. For each:
- Trigger spec-22 fan-out: per-recipient rows, preference-honoring, best-effort email, 25-recipient cap → digest above that.
- Both in-app notification AND Brevo email. Email content (exact contract, localized per Brevo template `account_deletion_blast_radius`):

> ⚠️ **Resource pending deletion**
>
> **{requester.DisplayName}** ({requester.Email}) has requested account deletion.
>
> The following resources you share will be **permanently deleted on {scheduledPurgeAtUtc}**:
> - {Workspace #1 Name} (owned by requester, you are a member, N other users)
> - {Project #1 Name} (in Workspace #1, you are a ProjectManager, K tasks affected)
> - {list continues, max 10 items, link to full list in web app}
>
> **Action required before {date}**: Save or export any data you need. After this date **this data cannot be recovered** by you or by support.
>
> If you are the Admin of one of these resources, you may export the resource in Settings → Export before the date. Shared comments/tasks authored by other users on these resources will also be deleted (they belong to the resource).
>
> This notice is sent automatically; replies are not monitored.

Notification fan-out is best-effort (Brevo best-effort, same as registration); a delivery failure does NOT block the approval. Each failed send is ErrorLogs + AuditLogs, and SuperAdmin sees a "N fan-out recipients failed" alert on the request row.

### 9f. Read-mostly contract during DeletionPending

While `AccountDeletionRequests.Status ∈ {DeletionRequested, ExportAvailable, PurgeScheduled}` (collectively "DeletionPending"), the user account is **read-mostly**. Controlled by `Deletion:ReadMostly403DuringPending` (default true).

**Allowed actions** (200/202 as normal):
- login, refresh, logout, OTP request/verify (for step-up)
- All GET/HEAD reads: dashboards, workspaces, projects, boards, tasks, comments, attachments, notifications, activity, profile settings
- account-export download (§9d)
- cancel-deletion request (`POST …/deletion-request/{id}/cancel`)

**Blocked actions** — return **403** `{error:"account_deletion_pending", message:"Account is pending deletion and is read-only. Cancel the deletion request in Settings to make changes.", scheduledPurgeAtUtc}`:
- All POST/PUT/PATCH/DELETE mutations: create/edit/delete workspaces, projects, boards, columns, tasks, comments, attachments; invite members; change roles; modify account settings except cancel; all org lifecycle endpoints; AI-triggered mutations (already 403 for AI, this adds human 403)
- Password change → redirect to forgot-password flow 403 same shape
- New OTP `delete_account` request → 409 `{error:"open_request_exists"}` (partial unique index blocks this at DB level too)

SuperAdmin can manually lift read-mostly on a pending request via `POST /api/admin/account-deletion-requests/{id}/lift-readonly` (step-up + note required).

### 9g. Entity-by-entity purge / re-home / tombstone rules — ZERO ORPHANS GUARANTEE

The purge job runs as idempotent resumable background work (spec-33 pattern: chunked bulk deletes per spec 41 revision; checkpoint per chunk; kill-switch inside the window via `POST /api/admin/account-deletion-requests/{id}/cancel-purge` → back to None with audit). Each chunk is one transaction with rollback. Order is leaves-first (dependents deleted before parents) so FKs never dangle. Re-home happens before delete so no owner-less rows exist even transiently.

**DELETED_USER_SENTINEL_GUID = `00000000-0000-0000-0000-000000000001`** — bootstrapped by the multi-tenant migration, never a real user, `DisplayName = "Deleted User"`. Used for tombstoning so FK constraints never fail.

| Entity (table) | Purge action when requester is involved | FK integrity guarantee |
|---|---|---|
| **`WorkspaceMembers`** WHERE UserId = requester | DELETE | Workspace must already have ≥1 other member AFTER this delete; if not → §9h cascade-re-home or cascade-delete the workspace first |
| **`Workspaces`** WHERE OwnerId = requester | Re-home OwnerId in priority: (a) oldest remaining `OrganizationRole = Owner/Admin` in the same Company, (b) oldest remaining `WorkspaceMember` with `WorkspaceRole = Owner/Admin`, (c) DELETED_USER_SENTINEL_GUID (last-resort tombstone). If workspace has ZERO members after removing requester → **cascade delete the workspace and all its children** (projects, boards, columns, tasks, comments, attachments — see below; blast-radius email already warned co-users) | OwnerId NOT NULL guarantee always holds |
| **`Projects`** WHERE OwnerId = requester | Re-home → Workspace.OwnerId; if that's also requester or Sentinel → Organization.OwnerId; if still requester → Sentinel. If project is in a cascade-deleted workspace → delete via FK cascade (no re-home needed) | OwnerId NOT NULL guarantee |
| **`Boards` / `Columns`** via ProjectId FK | Cascade DELETE or cascade re-home via Project's outcome — no separate logic needed | FK Cascade on Project |
| **`TaskItems`** WHERE AssigneeId = requester | SET AssigneeId = NULL (column is nullable). WHERE AuthorId = requester → keep AuthorId (task is owned by project, not author; if project purges, task goes with it). WHERE CreatedBy = requester → keep CreatedBy (same logic) | Nullable column → safe |
| **`TaskItems`** in cascade-deleted workspace/project | Cascade DELETE via FK | FK Cascade on Board/Column |
| **`Comments`** WHERE AuthorId = requester | **TOMBSTONE:** AuthorId → DELETED_USER_SENTINEL_GUID; Body → `"[Comment removed — account deleted]"`; `Edited = true`; `Attachments` collection cleared. `ThreadId` / `ReplyToCommentId` preserved so the conversation thread never breaks | FK to AuthorId never dangles |
| **`Comments`** in cascade-deleted tasks | Cascade DELETE via FK on TaskItem | FK Cascade |
| **`Attachments`** WHERE UploadedBy = requester | Delete physical blob (backend 11 blob storage, if configured; if blob not yet available v1 → set BlobKey = null + UploadedBy = Sentinel), DELETE row. Attachments on cascade-deleted tasks → cascade DELETE + blob delete | FK + blob integrity |
| **`Invites`** WHERE InvitedBy = requester OR `InvitedEmail = requester.Email` | DELETE (pending invites expire; accepted invites already have Member rows → those Member rows deleted separately) | Invites table has no hard FK to InvitedBy |
| **`Notifications`** WHERE UserId = requester | DELETE (private to requester; no cross-user FKs) | Standalone table |
| **`ProjectClients`** WHERE UserId = requester | DELETE + `RemovedAt = now` (soft trace) | FK Cascade |
| **`ClientFeedback`** WHERE AuthorUserId = requester | TOMBSTONE: AuthorUserId → Sentinel; Body → `"[Feedback removed — account deleted]"`; Status stays as-is; `PmResponse` preserved | FK integrity |
| **`ProjectHandoffs`** WHERE InitiatedBy = requester | Re-home InitiatedBy → Project.OwnerId; if that's requester → Workspace → Org → Sentinel | FK integrity |
| **`HandoffDocuments`** WHERE UploadedBy = requester | Re-home UploadedBy → Handoff.Project.OwnerId chain → Sentinel. Blob stays (it's a handoff deliverable owned by the project) | FK integrity |
| **`Roles`** WHERE CreatedBy = requester (future column) OR roles the requester created | CreatedBy → Organization.OwnerId; role rows stay (other users may have CustomRoleId → deleting them would break OrganizationMembers) | FK integrity |
| **`OrganizationMembers`** WHERE UserId = requester | DELETE (all orgs — requester removed from every company) | App-managed DELETE of membership rows (org FK is NO ACTION) |
| **`OrganizationInvites`** WHERE InvitedBy = requester OR Email = requester | DELETE | Standalone FK |
| **`OrganizationLifecycleEvents`** WHERE ActorUserId = requester | Keep rows; ActorUserId → Sentinel (audit trail survives; requester actor is anonymized) | FK integrity |
| **`AccountDeletionRequests`** WHERE UserId = requester AND Status = PurgeScheduled | Set `Status = Purged`, `PurgedAt = now`; do NOT DELETE the row (audit trail for 365 days; spec-20 prune proc removes it after tail) | FK survival |
| **`Users.Id = requester`** (last step, after all rows above) | **SOFT-DELETE ONLY, NEVER HARD-DELETE.** Set: `DeletedAt = now`, `DisplayName = "Deleted User"`, `Email = CONCAT('deleted_', LOWER(SUBSTRING(REPLACE(CONVERT(varchar(256), HASHBYTES('SHA2_256', CONCAT(CAST(Id AS varchar), @salt, CAST(DeletedAt AS varchar)))), 3, 32)), '@deleted.local')`, `AvatarUrl = NULL`, `PasswordHash = NULL`, `TwoFactorMethod = None`, `EmailVerified = false`, `PlatformRole = User`, `DeletionRequestId = NULL`. **All refresh tokens revoked** (RevokedAt = now). After this step: login → 401; display in member lists → "Deleted User"; old mentions (comments authored by Sentinel) read as "[Deleted User]". The row survives for FK integrity + compliance tail. Hard-delete of the Users row is NEVER performed within the bootcamp's 365-day audit tail. | FK integrity on all references to Users.Id |

### 9h. Fallback re-home chain — any entity not listed above

Any table with a `CreatedBy` / `UpdatedBy` / `AssigneeId` / `UploadedBy` / `ActorUserId` FK → `Users.Id`:
- If column is NULLABLE → set to NULL
- If column is NOT NULL → re-home in priority: Workspace.OwnerId → Project.OwnerId → Organization.OwnerId → DELETED_USER_SENTINEL_GUID. If still non-nullable and cascade allows → DELETE row with its parent cascade. If none of the above → **block purge** and alert SuperAdmin (manual review); automated purge never produces an FK violation, even if it means halting and alerting.

### 9i. Schema delta (ERD amendment v2 delta — Figma Make approval required before migration code, hard rule 4)

`AccountDeletionRequests`: `Id`, `UserId` FK `Users.Id` + `IX_AccountDeletionRequests_UserId`, **partial unique index** `UX_AccountDeletionRequests_OneOpenPerUser ON AccountDeletionRequests(UserId) WHERE Status IN ('Requested','Approved','ExportAvailable','PurgeScheduled')`, `Reason` nvarchar(2000) NOT NULL, `Status` (`DeletionRequestStatus` enum, append-only: `Requested`, `Approved`, `Rejected`, `Cancelled`, `ExportAvailable`, `PurgeScheduled`, `Purged`), `RequestedAt` NOT NULL, `DecidedBy?` FK `Users.Id`, `DecidedAt?`, `DecisionNote?` nvarchar(2000), `ExportBlobKey?` (backend 11), `ScheduledPurgeAtUtc?` NOT NULL once set, `PurgedAt?`, `CancelReason?` nvarchar(2000).
`Users.DeletionRequestId?` FK `AccountDeletionRequests.Id` (read-mostly flag; `Users.DeletedAt` still set ONLY by the purge job at §9g last step — never by the request route).
`NotificationType.AccountDeletion` (spec 22 enum revision).
LifecycleEventKind adds 7 user-lifecycle values: `AccountDeletionRequested`, `AccountDeletionApproved`, `AccountDeletionRejected`, `AccountDeletionExported`, `AccountDeletionPurgeScheduled`, `AccountDeletionPurgeCancelled`, `AccountDeletionPurged` (spec 21 trigger list unchanged — triggers stay on operational tables, app audit owns these LifecycleEvent rows).

## 10. Destructive-operation safety contract — rollback + confirm + 10-second countdown + cancel — PLANNED

Applies to every destructive operation (account-deletion request submit, org offboard/suspend/transfer, workspace/project/board/column/task/comment/attachment/member/role/client deletes, purge executions). The countdown is UX; the server owns the truth — the client never authorizes deletion by waiting.

### 10a. Two-phase server protocol (intent → confirm) with full rollback

**Phase 1 — Intent (server):** The destructive "Delete" button first calls an intent route (pattern: `POST /api/{entity}/{id}/delete-intent`, or the existing delete route in `?mode=intent`). The server:
1. Validates auth + membership + `org_suspended`/`account_deletion_pending` flags + step-up claim where required (§8).
2. Computes and returns the **blast radius summary** (`{workspaces:N, projects:M, tasks:K, comments:J, attachments:L, affectedUsers:X}`).
3. Writes an `AuditLogs` row: `Action = {Entity}.Delete.Intent`, `EntityId`, `ActorId`, `PayloadJson.blastRadius`, `PayloadJson.intentId`.
4. Returns **200** `{intentId, expiresAtUtc: now+10min, confirmAfterUtc: now+10s, blastRadius}`. **Nothing is deleted.** No transaction, no domain change.

The visible 10-second countdown reflects exactly `confirmAfterUtc`. If the client submits confirm BEFORE this time → server rejects with **409** `{error:"confirm_too_early", retryAtUtc}`. No way to skip the 10s.

**Phase 2 — Commit (server, single transaction with rollback):** After the countdown, the client sends confirm: `POST /api/{entity}/{id}/delete-confirm {intentId}` (+ step-up claim where required). The server:
1. Re-validates EVERYTHING from Phase 1 again (auth, membership, step-up, org/account flags, intent not expired, intent not cancelled). This is NOT a trust-the-client step.
2. Marks the intent as `Committing` in-memory (or on an intent table if/when shipped) so concurrent cancel → 409 `{error:"already_committing"}`.
3. Opens ONE SQL transaction (`BEGIN TRANSACTION` — either EF Core `DbContextTransaction` or Dapper `SqlTransaction`).
4. Executes the domain DELETE(s) inside the transaction (leaves-first order per §9g for account purge, entity FK cascade order for single-row deletes).
5. Writes `IAuditService` `{Entity}.Delete` + JSON Before/After diff **in the same transaction** (queued into `GriotDbContext.ChangeTracker` so `SaveChanges` commits domain + audit atomically).
6. Writes `ActivityLogs` row (user-facing feed, e.g. "Alice deleted task X") in the same transaction.
7. `COMMIT TRANSACTION` → **200/204** success.

**Full rollback guarantee (no partial deletes, no orphan FKs, no partial audit):** If ANY step in 4–6 throws (SQL FK violation, OOM, DB trigger `RAISERROR` per spec 21, deadlock, network split during commit), the ENTIRE transaction rolls back to zero state changes. Response: **500** `{error:"delete_failed", traceId, requestId, retryable:true/false}`. The client shows zero optimistic delete and surfaces the error with the request ID for support.

### 10b. Cancel protocol (works only before commit starts)

Client (user clicks Cancel on the dialog) → `POST /api/{entity}/{id}/delete-cancel {intentId}`. Server:
- Validates auth, intent not expired, intent not already `Committing`.
- Writes `AuditLogs` row: `{Entity}.Delete.Cancel` + `PayloadJson.cancelReason?` (if user supplied).
- Returns **200** `{cancelled:true}`. Subsequent confirm on that intentId → **409** `{error:"intent_cancelled"}`.

Intent expiry (10 min after issue) has the same effect as cancel: server treats expired intents as `Cancelled` with `PayloadJson.reason = "expired"`, and confirm → 409 `{error:"intent_expired"}`.

### 10c. Cancel-disable timing — explicit (your requirement: countdown = 1 → cancel disabled)

**Server truth:** Cancel is 200 up until the exact HTTP request that carries `delete-confirm` begins processing on the server (the moment Phase 2 step 2 marks intent as `Committing`). After that → 409 `already_committing`.

**Client enforcement (UX to prevent the race you called out):**
The client-side countdown dialog labels the confirm button as `[Confirm Delete in 10… 9… 8…]` (countdown seconds in the button text using `confirmAfterUtc - now`). **When the countdown display shows exactly `1` (one) second remaining → the Cancel button is PERMANENTLY DISABLED for this dialog.** Disablement reason (tooltip): *"Deletion is about to start and can no longer be cancelled."*

Why `1` remaining = disable (not `0`): The 1-second display → 0 transition coincides with the user being able to click "Confirm Delete". If Cancel were still enabled at `0`, a fast clicker could press Cancel AND Confirm in the same sub-second frame, racing the server's `Committing` mark. Disabling at `1` eliminates that human-observable race window.

The `confirmAfterUtc` returned by intent is `now + 10s`. The client disables Cancel at `confirmAfterUtc - 1s` = `now + 9s` after intent. If the user somehow clicks "Cancel" via browser dev-tools override at exactly 0.5s remaining → server is still in pre-commit state → cancel honoured (200). But the visible button is gone so the normal path has zero ambiguity. **Confirm button itself stays disabled (grayed) until countdown reaches 0 → label becomes `Confirm Delete` → clickable.**

### 10d. Intent / Confirm / Cancel → Audit + DB trigger coverage matrix

Every lifecycle step leaves both app-level audit (IAuditService, primary, high-fidelity with real ActorId) AND DB-level catch-all triggers (spec 21, `DB.`-prefixed, fires even if app code is bypassed):

| Lifecycle step | IAuditService row (app, unprefixed Action) | DB trigger rows (DB. prefixed — spec 21) |
|---|---|---|
| User clicks "Delete X" → `POST …/delete-intent` | `{Entity}.Delete.Intent` | If `DeleteIntents` table ships → `DB.DeleteIntents.INS` on row insert; if intent-tracking is in-memory only → app audit is sole coverage (acceptable — intent is non-destructive). Triggers on any other operational tables do NOT fire (no domain rows touched). |
| User clicks Cancel (countdown ≥ 2) → `POST …/delete-cancel` | `{Entity}.Delete.Cancel` | If `DeleteIntents` → `DB.DeleteIntents.UPD` (Status → Cancelled). If in-memory → app audit sole coverage. No domain rows touched. |
| Intent expires at 10 min | Scheduled sweep writes `{Entity}.Delete.IntentExpired` | If `DeleteIntents` → `DB.DeleteIntents.UPD`. No domain rows touched. |
| Countdown 0 → user clicks Confirm → Phase 2 starts | `{Entity}.Delete.ConfirmStart` (written BEFORE the transaction so if Step 4 throws, we still have a trace of the attempt). No DB triggers here — no domain rows touched yet. |
| Phase 2, Step 4: SQL DELETE executes inside transaction | (queued same-transaction) | **`DB.{table}.DEL` trigger fires for EVERY affected row** per spec 21 audit triggers. Triggers run inside the same transaction — a trigger `RAISERROR` rolls back the whole Step 4–6 block. Triggers write `ActorId` from `SESSION_CONTEXT(N'ActorId')` (set by API middleware); `OrganizationId` from `SESSION_CONTEXT(N'ActorOrgId')`. Before/After JSON from `deleted`/`inserted` pseudo-tables. |
| Phase 2, Step 5: IAuditService `{Entity}.Delete` + Before/After JSON diff | Written inside transaction → commits with domain | (App row is unprefixed; same `AuditLogs` table. Dedupe convention: app = no prefix, trigger = `DB.` prefix. Trigger rows are a catch-all lower-fidelity duplicate of app rows. Both are retained for 365-day tail (spec 20).) |
| Phase 2, Step 6: ActivityLogs (user feed) | `ActivityLogs` row (separate table, separate triggers if spec 21 covers it; spec 21 scope is operational tables only) |
| Phase 2, Step 7: `COMMIT TRANSACTION` succeeds | (All app + trigger rows committed atomically.) |
| Phase 2, ANY step throws → `ROLLBACK TRANSACTION` | `{Entity}.Delete.Failed` (written outside the rolled-back transaction, separate conn, includes `ExceptionType`, `Message`, `StackTrace` truncated) | (Trigger rows rolled back with the transaction — this is correct and intended: rollback means NO domain change, so no DB.* records of a change that didn't happen. The `.Failed` app audit row survives as incident evidence.) |

### 10e. Client confirmation dialog content — explicit (web + mobile implement identically)

Web ships one reusable `DestructiveConfirmDialog(intentId, confirmAfterUtc, blastRadius, entityLabel, scheduledPurgeDate?, onConfirm, onCancel)`; mobile ships same contract natively. qa spec 14 asserts the timings + race scenarios (confirm-after-cancel → 409, cancel-after-commit-start → 409, no orphan rows in DB). **No optimistic delete on any client.** UI state does NOT remove the row locally before the server returns 2xx/204.

```
┌──────────────────────────────────────────────────────────┐
│ 🗑️  Delete {entityLabel}?                                │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  This action CANNOT be undone.                           │
│                                                          │
│  ┌─ Blast radius ────────────────────────────────────┐  │
│  │  Affects {blastRadius.affectedUsers} other users. │  │
│  │  Workspaces: {N}  Projects: {M}  Tasks: {K}       │  │
│  │  Comments: {J}  Attachments: {L}                  │  │
│  └──────────────────────────────────────────────────┘  │
│                                                          │
│  {#if scheduledPurgeDate}                               │
│  ⚠️ Data will be PERMANENTLY DELETED on                 │
│  {scheduledPurgeDate}. Save / export before that date.  │
│  {/if}                                                  │
│                                                          │
│  {#if isAccountDelete}                                  │
│  💡 You will be able to DOWNLOAD YOUR DATA after        │
│  SuperAdmin approves the request. The request goes      │
│  through review, not instant deletion.                  │
│  {/if}                                                  │
│                                                          │
│  ┌──────────────────┐   ┌──────────────────────────┐    │
│  │     Cancel       │   │ Confirm Delete in 10… 9 │    │
│  │ (enabled until   │   │ (disabled until count=0 │    │
│  │  countdown=1,    │   │  then becomes clickable │    │
│  │  then DISABLED)  │   │  with label "Confirm")  │    │
│  └──────────────────┘   └──────────────────────────┘    │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

Cancel button behavior:
- **`countdown > 1`** → enabled, clickable. Clicking dismisses the dialog, fires `delete-cancel` in the background, shows toast "Deletion cancelled."
- **`countdown == 1`** → **disabled + grayed + crossed-out icon**, tooltip "Deletion is about to start and can no longer be cancelled."
- **`countdown == 0` after confirm sent** → Cancel stays disabled, shows spinner in confirm button, status "Deleting…"
- Server responds 2xx → dialog closes, toast "{Entity} deleted." (optimistic row removal in UI NOW, not before)
- Server responds 4xx/5xx → dialog stays open, error toast with `requestId`, user can retry

### 10f. DB triggers (spec 21) capture this

As spec 21 multi-tenant update already states, spec-21 triggers on `AFTER INSERT/UPDATE/DELETE` fire on ALL operational tables, writing `DB.`-prefixed `AuditLogs` rows with `SESSION_CONTEXT(N'ActorId')` and `SESSION_CONTEXT(N'ActorOrgId')`. The account-deletion purge job (§9g) runs in chunks — each chunk is one transaction, triggers fire per deleted row in the chunk, and the spec-20 prune proc retains the 365-day tail of both app and `DB.` rows with no recursion (triggers never fire on `AuditLogs`/`ApiLogs`/`ErrorLogs` themselves nor on `RefreshTokens`/`OtpChallenges`).

## 11. Implementation-readiness checklist (what "completely setup ready" means)

Each row names the artifact that must exist **before production code starts**, plus an explicit **acceptance criterion** (what the reviewer must verify to mark this gate ✅). Planned behavior is not acceptance evidence until its owning spec ships on its own branch/PR (hard rule, unchanged).

**Legend:** ✅ = artifact exists + criterion met · ⚠️ = artifact exists but criterion NOT met · ❌ = artifact missing · **Owner spec** = branch that owns the work (one branch per spec, no batching, hard rule 6).

| # | Gate | Artifact that must exist | Explicit acceptance criterion (pass/fail for reviewer) | Owner spec |
|---|---|---|---|---|
| 1 | ERD amendment v2 APPROVED in Figma Make, exported to `diagrams/erd/` | `diagrams/erd/multi-tenant-amendment.md` §2 entity table, §10 AccountDeletionRequests entity + FK, §26 enums (DeletionRequestStatus + NotificationType.AccountDeletion + 7 user-lifecycle LifecycleEventKind values) | Figma Make ERD shows every entity in §2 + §9i table with correct fields; `AccountDeletionRequests` has partial unique index; FK arrows match §9i; enum members match exactly. Exported file in repo matches Figma Make visually (diff of entity lists). Hard rule 4: NO migration code in any PR unless this gate = ✅ FIRST | 29 |
| 2 | OTP/step-up routes + `delete_account`/`step_up` purposes + `RequireStepUp` guard + `RejectAiOnAuthSurface()` + SUPERSESSION of instant delete (HTTP 410) | `backend/project-kit/feature-specs/23-critical-action-otp-step-up.md` Contract changes table + `docs/api/auth-contract.md` PLANNED section | Route table in spec 23 contains all 13 routes in auth-contract.md §PLANNED table (forgot-password / reset-password / deletion-request / cancel / admin list / admin approve / admin reject / export + old DELETE→410). `RejectAiOnAuthSurface()` guard listed in Implementation notes; xUnit test plan for 403 per route present; step-up allow-list matches §8 allow-list exactly; SUPERSCRIPTION banner at top of Contract changes says "v1 DELETE route is REMOVED, returns 410" | 23 |
| 3 | Deletion-request routes + SuperAdmin review routes + export route + 30-day purge job (§9b formula) + blast-radius fan-out + re-home/tombstone cascade (§9g table) + read-mostly 403 during Pending | `backend/project-kit/feature-specs/23-…` (request/review/export) + `33-…` (purge-job pattern) + `22-…` (fan-out) | §9b formula present in spec 23 AC as a unit-test scenario: request sent day0 approved day29 → scheduled purge clamped to day36; MinPurgeWindowDays floor enforced. §9g entity-by-entity table copied verbatim to spec 33 as purge order AC. §9e blast-radius email template contract copied to spec 22 as fan-out content AC (SAVE/EXPORT instruction present). §9f read-mostly 403 + allowed/blocked lists copied to spec 23 AC with HTTP status+body shape | 23 / 33 / 22 |
| 4 | DB trigger coverage (§10d matrix + §10f) — org-stamped, no-recursion guards, same-transaction app audit, prune non-fighting | `backend/project-kit/feature-specs/21-database-resilience-backups-triggers.md` + `backend/src/Griot.Infrastructure/Sql/audit-triggers.sql` (idempotent) | Triggers exist on 4 v1 tables + all new tenant tables (OrganizationMembers, Roles, OrganizationInvites, ProjectClients, ClientFeedback) per spec 21 §88. Trigger SQL writes `SESSION_CONTEXT(N'ActorId')` + `SESSION_CONTEXT(N'ActorOrgId')`. No-recursion guard: triggers skip on `AuditLogs`/`ApiLogs`/`ErrorLogs`/`RefreshTokens`/`OtpChallenges` (asserted by test). Prune proc (spec 20/51) does NOT fight triggers: integration test runs both + no double-write, no recursion error. §10d trigger rows for `DB.{table}.DEL` present in AC | 21 |
| 5 | Company lifecycle routes (onboard / suspend / reactivate / transfer-ownership / offboard) with step-up on destroys + OrganizationLifecycleEvents audit | `backend/project-kit/feature-specs/32-…` + `33-…` | Every destroy/suspend/transfer/offboard route in spec 32 has `RequireStepUp(action)` in controller pseudocode. Step-up claim org-binding (§8) enforced (org A claim can't act on org B). `OrganizationLifecycleEvents` rows written in same-transaction for each lifecycle endpoint in spec 32; 7 user-lifecycle values from §2 present | 32 / 33 |
| 6 | Client portal + handoff/maintenance reads + deletion notice NEVER leaks internal board internals to clients | `backend/project-kit/feature-specs/34-…` + `35-…` | Spec 34 `GET /api/client/projects/{id}/progress` response excludes Column order, task internal description, and PM-only comments (asserted by xUnit with `ClientFeedbackStatus.New`). Deletion notice (spec 22 fan-out §9e) sends progress-only variant to ProjectClient recipients (board-internal rows stripped from resource list in email). Client-portal settings screen exports only Progress-view data (client can't export the full board) | 34 / 35 |
| 7 | Destructive-confirm dialog on EVERY destructive button (§10a–10e) — 10s countdown, Cancel disabled at countdown=1, NO optimistic delete + web/mobile parity | web 07/13/14 feature specs + mobile 06/08/09 specs + `qa/` test plan 14 | Web ships `DestructiveConfirmDialog` component with API matching §10e (intentId, confirmAfterUtc, blastRadius, entityLabel, scheduledPurgeDate). Mobile spec has 1:1 native widget with same contract. qa 14 has 4 tests: (a) confirm sent 9.5s after intent → server 409 `confirm_too_early`; (b) cancel sent 2s remaining → server 200 then confirm → server 409 `intent_cancelled`; (c) cancel sent 0.3s remaining (race) → server 200 allowed BUT client widget disabled cancel; (d) commit fails → NO optimistic delete visible in UI after 500 response | web / mobile |
| **8** | **Purge-window formula + MinPurgeWindowDays guard DOCUMENTED and TESTED** | This guide §9b + spec 23 AC + env var in `integration-contracts.md` | §9b formula matches `integration-contracts.md` env `Deletion:PurgeWindowDays=30` + `Deletion:MinPurgeWindowDays=7`. Spec 23 xUnit test: `requestedAtUtc + 30d` normal case; `requestedAtUtc+29d approval` extends to approval+7d; SuperAdmin override to 91d clamped to 90; override to 6d clamped to 7 | 23 |
| **9** | **Entity-by-entity purge/re-home/tombstone TABLE (§9g) + FK integrity tests** | This guide §9g + §9h + spec 33 purge-job AC + qa 14 integration tests | §9g 24 rows match spec 33's purge order exactly (leaves-first). Integration test seeds a fully-loaded workspace with 5 users, 100 tasks, 500 comments, 50 attachments, runs purge for requester UserId=2, checks: (a) zero FK violations (DBCC), (b) DELETED_USER_SENTINEL_GUID is AuthorId of exactly N requester-authored comments, (c) Workspace originally owned by requester now has OwnerId = oldest remaining Admin (not Sentinel), (d) Users row for requester has DeletedAt set + Email LIKE 'deleted_%@deleted.local' + PasswordHash = NULL | 33 / qa14 |
| **10** | **Blast-radius email + notification content CONTRACT (§9e) enforced in fan-out** | This guide §9e email template text + spec 22 fan-out AC | Spec 22 acceptance has a string-contains assertion for every co-user email body: (1) the exact phrase "Save or export any data you need before that date" present; (2) `scheduledPurgeAtUtc` ISO date present in both subject + body; (3) for 26 recipients, response is a digest (not 26 individual emails) AND digest lists resource count + date; (4) SuperAdmin sees `FanOutFailuresCount` on request row when Brevo rejects one recipient | 22 |
| 11 | **Contract-sync green + system verification gates per owning spec** | Terminal output of: `python3 scripts/check-contract-sync.py` (repo root) + per-system: `backend: dotnet build + dotnet test; web: lint+typecheck+test+build; mobile: analyze+test; ai/mcp: lint+typecheck+test` | `check-contract-sync.py` exits 0 with ZERO "stale contract" warnings (every route/env/entity/enum in this guide ALSO appears in integration-contracts.md and owning spec). Per-system verification gates all green (no build/test failures) on the owning spec's branch BEFORE PR | all / each branch |

**How to use this checklist (process per hard rule 6):**

1. Checkout feature branch for ONE spec (e.g. `feature/backend/23-critical-action-otp-step-up`).
2. Before writing ANY production code → confirm all gates owned by PREVIOUS specs are ✅. If not → stop, go back, don't skip gates.
3. Write code for the spec → complete gates owned by THIS spec.
4. Run row 11 (contract-sync + build/test). Fix until green.
5. Update `{system}/project-kit/context/progress-tracker.md` for the owning spec → mark all AC as pass.
6. **WAIT FOR USER APPROVAL** (hard rule) after committing to GitHub.
7. After user approval → push to GitHub, open PR, merge, then → next spec's branch.

**Branching reminder (unchanged hard rule 6):** one spec per branch/PR (`feature/backend/23-…`, `feature/backend/21-…`, …). This guide's §8–§11 text ships incrementally with the spec branch that owns each behavior (23 carries §8+§9, 21 carries the trigger half of §10, web/mobile carry the dialog half) — never as a code change on its own. This edit assembles the full contract now so reviewers approve the shape once.

## 12. Out-of-scope confirmations (this wave)

TOTP/passkeys · SMS/WhatsApp/push channels (Email-only per spec 12) · payments/billing · SCIM 2.0 · per-request purge windows shorter than 7 days or longer than 90 · AI involvement in auth/OTP/deletion decisions (human-only, permanently) · physical tenant isolation (Bridge/Silo deferred, §1).

## 13. Payments — explicitly out of scope this wave

Research §7 is recorded but deferred: `OrganizationPlan` is display metadata only; no Stripe/subscriptions in this wave (a future wave re-enters through `docs/planning/IMPLEMENTATION-ROADMAP.md`).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
