# Integration Contracts (cross-system)

These contracts are owned cross-system. Change one and the contract-sync gate (`/.agents/skills/contract-sync`) fires.

## Ports (host → container)

| Service | Container | Host:Container | Notes |
|---|---|---|---|
| sababisha-sqlserver | SQL Server 2022 | 14333:1433 | primary; `sababisha_*` volumes |
| sababisha-postgres | PostgreSQL 16 | 5433:5432 | secondary/test |
| sababisha-redis | Redis 7 | 6380:6379 | auth support |
| api | Griot.Api | 8080:8080 | `ASPNETCORE_URLS=http://+:8080` |
| mcp | Griot MCP | 3001:3001 | Streamable HTTP |

## Environment variables

| Scope | Var | Purpose |
|---|---|---|
| backend | `ConnectionStrings__Default` | SQL Server (compose: `Server=sababisha-sqlserver,1433;Database=griot;User Id=sa;Password=…`) |
| backend | `JWT__Key` `JWT__Issuer` `JWT__Audience` (+ `JWT__Key_Previous` rotation window — spec 30 ✅) | Token sign/validate |
| backend | `Redis__Connection` | `sababisha-redis:6379` |
| backend | `GRIOT_SERVICE_TOKEN` | AI/MCP service calls (Bearer `Authorization: Bearer {token}` + `X-On-Behalf-Of: {real User.Id}` header — OBO) |
| backend | `Cors__AllowedOrigins` | Vercel origin prod; localhost dev |
| backend | `BREVO_API_KEY` | Brevo SMTP/API key (`xkeysib-…`); also `Brevo__ApiKey` |
| backend | `BREVO_FROM_EMAIL` `BREVO_FROM_NAME` | THE single Brevo-verified sender + display name (2026-09-11: `Brevo:Senders:<Key>` profile map removed — one sender for every email purpose) |
| backend | `CLOUDINARY_URL` (or `CLOUDINARY_CLOUD_NAME` `_API_KEY` `_API_SECRET`) | Attachment blob storage via `CloudinaryDotNet` (server-to-server; secret never exposed to web/mobile) |
| backend | `Web__BaseUrl` | Web origin used in notification email deep links (spec 22, planned) |
| backend | `RateLimit__Auth__PermitLimit` etc. | Per-route limiter partitions (spec 19, planned; defaults documented in spec 19) |
| web | `VITE_API_URL` | deployed API base |
| ai/mcp | `GRIOT_API_URL` `GRIOT_SERVICE_TOKEN` | API access |
| ai | `ANTHROPIC_API_KEY`/`OPENAI_API_KEY` | LLM keys ONLY in `ai/.env` |
| backend | `TRIGGER_SECRET_KEY` | Backend → Trigger.dev REST enqueue (server-to-server; never exposed to web/mobile) |
| backend | `WEBHOOK_SECRET` | Trigger.dev → backend HMAC callbacks (`X-Trigger-Signature`; read by `WebhookHmacMiddleware` as `Webhook:Secret` ?? `WEBHOOK_SECRET`; blank values treated as missing) |
| backend | `Webhook:MaxBodyBytes` | Webhook payload cap (default 64 KiB → 413 over); replay protection PLANNED (signed freshness + event-id cache) |
| backend | `Security:StepUpTtlSeconds` / `Security:StepUpActions` | Critical-action step-up TTL + action allow-list (spec 23, PLANNED) |
| backend (PLANNED, 2026-09-11 wave) | `Deletion:PurgeWindowDays` (default 30) | Default scheduled-purge offset from **request date** (account-deletion §9b + org-offboarding). Single source of truth: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §9b |
| backend (PLANNED) | `Deletion:MinPurgeWindowDays` (default 7, floor=7, ceiling=90) | Floor on SuperAdmin per-request override; ensures co-users get at least 7 days notice. §9b clamp formula applied when approval leaves <7 days from request date |
| backend (PLANNED) | `Deletion:ReadMostly403DuringPending` (default true) | If true, ALL POST/PUT/PATCH/DELETE mutations EXCEPT cancel-deletion + export-download return 403 `{error:"account_deletion_pending",message:"…",scheduledPurgeAtUtc}` while `AccountDeletionRequests.Status ∈ {Requested,Approved,ExportAvailable,PurgeScheduled}`. Lifted per-request by SuperAdmin `lift-readonly` route (spec 23). §9f |
| backend | `SUPERADMIN__EMAIL` (+ `SUPERADMIN__PASSWORD` first-boot optional) | SuperAdmin bootstrap on startup (multi-tenant spec 30 ✅). `PlatformRole` upgraded idempotently + audit-logged. Password used ONLY on first-boot if no SuperAdmin row exists; subsequent boots ignore. |
| backend (PLANNED) | `Organizations:RetentionDays` (default 30) | Company-offboarding retention window (spec 33); separate from `Deletion:PurgeWindowDays` which is per-user account deletion |
| backend | `Security:LogRawTier` / `Alerts:Thresholds` | Raw-log tier allow-list (default SuperAdmin,Dev) + alert severity thresholds (specs 25/27, PLANNED) |
| backend | `SITE_URL` | Deployed origin (e.g. https://griot.vercel.app) used in brand email footers — NOT the sender domain (verify a domain you own; §7b) |
| web | `VITE_TRIGGER_ACCESS_TOKEN` | Realtime WS access token — read-only Copilot stream delivery only |
| infra | `SABABISHA_SA_PASSWORD` `SABABISHA_PG_PASSWORD` | local compose dev DB passwords |
| backend (recovery only) | `POSTGRES_RECOVERY_TARGET_TIME` | PITR restore target, set automatically on the NEW restored Railway PostgreSQL service (`<service>-restored-YYYYMMDD-HHMM`); NOT part of normal configuration — see Database recovery contract below |

## Communication contracts (spec 12 — own-stack)

Outbound messaging is **Email only** (SMS/WhatsApp/Contacts/automations removed from code
in the same branch). Email goes through `IEmailService` (`BrevoEmailService`,
`POST /v3/smtp/email`) with per-purpose sender identities `Brevo:Senders:<Key>`
(Key ∈ Security, Admin, NoReply, Support, Info, Team). Redis gate: OTP request
3/15min per email (`ratelimit:otp:request:{email}`) + API global limiter 100/min.
Best-effort delivery is scoped to **registration only** (register always returns 201);
`/api/auth/otp/request` is the one caller that surfaces Brevo delivery failure as
**HTTP 502** (202 on success, 401 unknown email, 429 rate-limited).
Canonical: `docs/communication/COMMUNICATION-GUIDE.md`; owner spec
`feature-specs/12-communication-channels-brevo.md`. No REST route changes.

## Database recovery contract (restored-service cutover)

Owned by `docs/planning/RUNBOOK-ROLLBACK.md` + infra spec 06. Railway managed PostgreSQL PITR (pgBackRest) provisions an **independent restored service** named `<service>-restored-YYYYMMDD-HHMM` with a NEW volume; environment variables are copied from the source EXCLUDING archive credentials, and `POSTGRES_RECOVERY_TARGET_TIME` is set automatically (recovery timestamp must fall within Railway's PITR retention window). The restored service replays the source WAL archive in **read-only mode** up to the target timestamp (`pgbackrest restore --type=time`); the source database is never written by recovery.

Validate before cutover: last `AuditLogs`/`ActivityLogs` timestamp vs the recovery target + smoke tests against the restored connection string. **Quiesce dependent-service writes** (maintenance/read-only mode) before switching `ConnectionStrings__Default` to the restored service — or document reconciliation of post-target writes — because any write to the source after `POSTGRES_RECOVERY_TARGET_TIME` is absent from the restored fork and is lost on cutover. Cutover then updates `ConnectionStrings__Default` on dependent Railway services (Vercel/Trigger env updated in the same step) and redeploys.

This recovery flow is **DISTINCT from normal SQL Server `ConnectionStrings__Default` configuration** — it applies only during a PITR recovery incident. PITR must already be enabled with its first post-enable base backup complete; enabling it after an incident provides NO historical restore window.

## REST route contract (owned by backend spec 04)

Prefix `/api`. Auth module `register/login/refresh/logout/otp/request/verify`; then `/workspaces`, `/workspaces/{id}/members`, `/invites/{token}/accept`, `/projects`, `/boards`, `/columns`, `/tasks`, `/tasks/bulk-status`, `/tasks/{id}/comments`, `/tasks/{id}/attachments`, `/activity`, `/notifications`, `/notifications/read-all`, `/dashboard/summary`, `/webhooks/trigger`. Full shapes in `backend/project-kit/context/api-surface.md`.

**2026-09-11 wave — PLANNED additive routes (no existing routes modified except DELETE /api/auth/account → 410):**
- Auth/account (backend spec 23): `forgot-password`, `reset-password`, **`/auth/account/deletion-request`** (POST + `/{id}/cancel`), **`/admin/account-deletion-requests`** (GET list + `/{id}/approve|reject|lift-readonly|cancel-purge`), **`/auth/account/export`** (GET). Old v1 `DELETE /api/auth/account` → HTTP **410 Gone** (superseded; response body includes pointer to new deletion-request workflow).
- **Destructive-op intent/confirm/cancel pattern** (every entity's delete surface, §10 two-phase protocol): For each existing or new entity endpoint `{entity}` (workspaces, projects, boards, columns, tasks, comments, attachments, invites, members, roles, clients, organizations): `POST /api/{entity}/{id}/delete-intent` → `POST /api/{entity}/{id}/delete-confirm` → `POST /api/{entity}/{id}/delete-cancel`. Server owns `confirmAfterUtc = now+10s` clock; cancel disallowed after commit starts (409 `already_committing`). GraphQL mirrors via `delete{Entity}Intent/Confirm/Cancel` mutations. Destructive-op lifecycle + cancel-disable-at-countdown=1 client policy detailed in `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §10a–§10e.

## GraphQL surface (owned by backend spec 05)

Queries: `me`, `workspace(id)`, `projects`, `board(id)`, `tasks(filter, sort)`, `notifications`, `dashboardSummary`, `workspaceReports`. (Includes Report entity from System Reports). Mutations mirror REST. Fields/entities named exactly per ERD.

## Multi-Tenant contract (2026-09-11 wave — PLANNED)

Canonical: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`; planned ERD amendment: `diagrams/erd/multi-tenant-amendment.md`; owner specs: backend 29–35 (+ revisions 36–51).

- **Tenant** = `Organization` (a Company). Pool model: shared schema, every tenant-owned table carries `OrganizationId`; isolation = `ITenantContext` (JWT `org` claim) + EF Core global query filters + repository guard (SQL Server has no RLS). `Users`/`RefreshTokens` stay global.
- **New entities** (ERD approval pending): `Organizations`, `OrganizationMembers`, `Roles`, `OrganizationInvites`, `ProjectClients`, `ClientFeedback`, `ProjectHandoffs`, `HandoffDocuments`, `OrganizationLifecycleEvents`, **`AccountDeletionRequests`** (user-initiated deletion request → SuperAdmin review → 30-day purge). New enums: `OrganizationStatus` (`Active`/`Suspended`/`Offboarding`/`Archived`), `OrganizationPlan`, `OrganizationRole` (`Owner`/`Admin`/`ProjectManager`/`Member`/`Client`/`Custom`), `PlatformRole`, `ClientFeedbackKind/Status`, `HandoffStatus`, `HandoffDocumentKind`, `LifecycleEventKind`, **`DeletionRequestStatus`** (`Requested`/`Approved`/`Rejected`/`Cancelled`/`ExportAvailable`/`PurgeScheduled`/`Purged` — append-only). **`NotificationType` gains `AccountDeletion`** (spec 22 fan-out for co-user blast-radius notices). `ProjectStatus` gains `Handoff`/`Maintenance`/`PostDeploymentSupport`.
- **Roles:** SuperAdmin = platform operator (onboards/suspends/offboards companies; `SUPERADMIN__EMAIL` bootstrap); Admin = company owner (ALL projects in own company only); ProjectManager = assigned projects; Member; Client = progress view + feedback. Admin/PM create custom roles from the fixed permission catalogue (`org.read, org.settings.manage, org.members.manage, org.roles.manage, org.projects.view_all, project.manage, task.manage, comment.write, client.manage, client.feedback.read, client.feedback.respond, report.generate, log.read_tier`).
- **JWT v2 (backend 30 — ✅ IMPLEMENTED 2026-09-12):** access token carries `name`, `org` (active OrganizationId), `role`, `perms`; 15-min HS256 unchanged; **`POST /api/auth/select-organization`** re-issues the pair on org switch; **refresh tokens stay opaque 64-hex (never a JWT — refresh semantics are unchanged)**. Signing key `JWT__Key` ≥ 64 chars (512 bits) in production, secret-store only.
- **Env additions (PLANNED):** `JWT__Key` policy, `SUPERADMIN__EMAIL`, `SUPERADMIN__PASSWORD` (first-boot), `Organizations:RetentionDays` (offboarding, default 30).
- **Platform/lifecycle routes:** `POST /api/organizations` (SuperAdmin onboard → 201 `{organization, ownerInvite}`), `GET /api/organizations[/{id}]`, `POST /api/organizations/{id}/suspend|reactivate|transfer-ownership`, `PUT /api/organizations/{id}/plan`, and `POST /api/organizations/invites/{token}/accept` — **IMPLEMENTED 2026-09-12 (backend 32)**; `POST /api/organizations/{id}/offboard` stays **PLANNED** (backend 33). Org-scoped `roles`/member-role routes are backend 31 (IMPLEMENTED); company-owner invites ride `OrganizationInvites` via backend 32. Client surface `GET /api/client/projects[/{id}/progress]`, `POST /api/client/projects/{id}/feedback`, handoff + maintenance routes (backend 35, PLANNED). Suspended orgs: writes 403 `org_suspended`, reads allowed (implemented backend 29/32 gate).

## Entities & enums (owned by the approved ERD)

`Users`, `Workspaces`, `WorkspaceMembers`, `Invites`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `ActivityLogs`, `Notifications`, `RefreshTokens`, `OtpChallenges`, `Reports`. Enums: `TaskStatus`, `Priority`, `WorkspaceRole`, `NotificationType`, `TwoFactorMethod`. These names are used verbatim by web TS types, mobile Dart models, GraphQL SDL, and MCP tool schemas.

## AI service-token & webhook contract (spec 09 — own-stack)

Canonical: `docs/api/ai-service-token-contract.md`; owner spec `backend/project-kit/feature-specs/09-ai-service-token-and-webhooks.md`; orchestration authority `research/ai-integration.md` §2a.

- **AI → .NET data plane (OBO):** `Authorization: Bearer {GRIOT_SERVICE_TOKEN}` **plus** `X-On-Behalf-Of: {Guid}` (real `Users.Id`) → `ServiceToken` auth scheme issues a **real-user On-Behalf-Of principal** — role `ai-on-behalf-of`, `auth_method=service_token_obo`, exactly four `scope` claims (`ReadWorkspace`, `CreateTask`, `AddComment`, `CreateNotification`). Constant-time token compare; missing/invalid header or unknown OBO user → 401. Routing: `MultiAuth` policy scheme forwards JWT-shaped bearers (2 dots) to `JwtBearer`, any other bearer to `ServiceToken`.
- **Restricted surface:** deletes, invites, member management and `PATCH /api/tasks/bulk-status` → **403** for AI OBO callers (`ForbidIfAiCall()`; GraphQL `deleteWorkspace`/`deleteProject`/`deleteTask` + admin/owner-only mutations reject). No virtual `ai-agent` member exists.
- **Trigger.dev → .NET webhook:** `WebhookHmacMiddleware` (before `UseAuthentication`) verifies `X-Trigger-Signature: sha256=<hex>` (HMAC-SHA256 of raw body) on `POST /api/webhooks/trigger`; 401 on mismatch/absent header, 503 if `Webhook:Secret`/`WEBHOOK_SECRET` unconfigured; controller returns 202.
- **.NET → Trigger.dev enqueue:** `TriggerDevClient` — `POST {Trigger:ApiUrl}/api/v1/tasks/{taskId}/trigger` with Bearer `TRIGGER_SECRET_KEY` (default API `https://api.trigger.dev`); **enqueue-after-persist** — failure is logged and returns `false`, never rolls back the domain write.

## AI/MCP tool contract

Tools (ids): `list_projects`, `list_boards`, `get_board`, `get_task`, `create_task`, `add_comment`, `get_activity_feed`, `summarize_project`. Agent operates as a Level 4 planning loop requiring human approval gates for multi-step execution. All write via backend GraphQL/REST with `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of` (real-user OBO principal — four scopes, no deletes/invites; spec 09).

## CI/CD contract

GitHub Actions job names (qa owns suites, infra owns pipeline): `test-dotnet`, `test-web`, `test-mobile`, `test-ai`, `test-mcp`, `newman`, `cypress`, `deploy`. Merge blocked if any red.

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Email-OTP 2FA implemented: `POST /api/auth/otp/request` (202 on success; Brevo delivery failure surfaces as 502; `email_verify` auto-sent on register) and `POST /api/auth/otp/verify` (200/401/429); sets `Users.EmailVerified`; branded Brevo template per purpose (`auth-contract.md`). Registration returns 201 after SQL persistence; malformed refresh returns 401 and authenticated logout remains 204.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
