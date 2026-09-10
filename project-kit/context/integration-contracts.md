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
| backend | `JWT__Key` `JWT__Issuer` `JWT__Audience` | Token sign/validate |
| backend | `Redis__Connection` | `sababisha-redis:6379` |
| backend | `GRIOT_SERVICE_TOKEN` | AI/MCP service calls (Bearer) |
| backend | `Cors__AllowedOrigins` | Vercel origin prod; localhost dev |
| backend | `BREVO_API_KEY` | Brevo SMTP/API key (`xkeysib-…`); also `Brevo__ApiKey` |
| backend | `BREVO_FROM_EMAIL` `BREVO_FROM_NAME` | Fallback sender when no profile set |
| backend | `BREVO_SENDER_<KEY>_EMAIL` `_NAME` `_REPLYTO` | Sender identities: SECURITY, ADMIN, NOREPLY, SUPPORT, INFO, TEAM |
| backend | `CLOUDINARY_URL` (or `CLOUDINARY_CLOUD_NAME` `_API_KEY` `_API_SECRET`) | Attachment blob storage via `CloudinaryDotNet` (server-to-server; secret never exposed to web/mobile) |
| web | `VITE_API_URL` | deployed API base |
| ai/mcp | `GRIOT_API_URL` `GRIOT_SERVICE_TOKEN` | API access |
| ai | `ANTHROPIC_API_KEY`/`OPENAI_API_KEY` | LLM keys ONLY in `ai/.env` |
| backend | `TRIGGER_SECRET_KEY` | Backend → Trigger.dev REST enqueue (server-to-server; never exposed to web/mobile) |
| backend | `WEBHOOK_SECRET` | Trigger.dev → backend HMAC callbacks (`X-Trigger-Signature`; read by WebhookController as `Webhook:Secret` ?? `WEBHOOK_SECRET`) |
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

## GraphQL surface (owned by backend spec 05)

Queries: `me`, `workspace(id)`, `projects`, `board(id)`, `tasks(filter, sort)`, `notifications`, `dashboardSummary`, `workspaceReports`. (Includes Report entity from System Reports). Mutations mirror REST. Fields/entities named exactly per ERD.

## Entities & enums (owned by the approved ERD)

`Users`, `Workspaces`, `WorkspaceMembers`, `Invites`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `ActivityLogs`, `Notifications`, `RefreshTokens`, `OtpChallenges`, `Reports`. Enums: `TaskStatus`, `Priority`, `WorkspaceRole`, `NotificationType`, `TwoFactorMethod`. These names are used verbatim by web TS types, mobile Dart models, GraphQL SDL, and MCP tool schemas.

## AI/MCP tool contract

Tools (ids): `list_projects`, `list_boards`, `get_board`, `get_task`, `create_task`, `update_task_status`, `add_comment`, `get_activity_feed`, `summarize_project`. Agent operates as a Level 4 planning loop requiring human approval gates for multi-step execution. All write via GraphQL with `GRIOT_SERVICE_TOKEN`; no deletes/invites.

## CI/CD contract

GitHub Actions job names (qa owns suites, infra owns pipeline): `test-dotnet`, `test-web`, `test-mobile`, `test-ai`, `test-mcp`, `newman`, `cypress`, `deploy`. Merge blocked if any red.

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Email-OTP 2FA implemented: `POST /api/auth/otp/request` (202 on success; Brevo delivery failure surfaces as 502; `email_verify` auto-sent on register) and `POST /api/auth/otp/verify` (200/401/429); sets `Users.EmailVerified`; branded Brevo template per purpose (`auth-contract.md`). Registration returns 201 after SQL persistence; malformed refresh returns 401 and authenticated logout remains 204.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
