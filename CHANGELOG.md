# Changelog

All notable changes follow [Keep a Changelog](https://keepachangelog.com/) + [Semantic Versioning](https://semver.org/).

## [Unreleased] — 2026-09-11 (AI superpowers & critical-action OTP planning wave [own-stack])

### Review corrections (2026-09-11)

- Clarify planned notification availability, backend 10 dependencies, report creation cursors, test-run completeness and backend-owned incident authorization/delivery controls.
- Synchronize the previously adopted Trigger.dev v4 contract and pin CLI/SDK/react-hooks to 4.5.16; v3 cloud is retired. Future AI/runtime features remain unimplemented.
- Record review dispositions and the user-approved one-time planning PR grouping exception in `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`.


### Added
- **PLANNED specs (planning artifacts only — no production code yet):** backend **23** (critical-action OTP & step-up: login 2FA enforcement, forgot/reset password, delete account, guarded-op step-up via `RequireStepUp`, human-only surface — AI OBO 403), backend **24** (AI reports & export surface: `Report` rows, PDF/CSV artifacts via blob spec 11, `audit-summary`, planned FIFTH OBO scope `CreateReport`), ai **06** (Copilot knowledge agent + system auditor), ai **07** (report generation PDF + CSV), ai **08** (advanced Level-4 executor), web **11** (AI Reports & Audit Center — award-grade UX), mcp **06** (v2 report/audit tools).
- **`docs/observability/HOW-LOGGING-WORKS.md`** — how logging works end-to-end: request lifecycle → four tables, correlation (`X-Request-Id` + ai runId), retention, worked example, AI attribution.
- **Backend spec 20 bumped** — async writers (bounded queue + workers), middleware order, spec-23/24 audit events, dev seeding, index validation.
- **Brevo sender-validation Q&A** (`COMMUNICATION-GUIDE` §7b + spec 12): `noreply@griot.app` fails because the From-domain is not verified; `griot.vercel.app` is Vercel-owned and cannot be authenticated; verify a domain YOU own (TXT `brevo-code`, optional DKIM/SPF; no MX needed); best format = one authenticated domain + the six fixed sender identities.
- **Contract sync:** roadmap P0 (… → 21 → 23 → 11 → 24 → 10) and P2 (ai 06–08 + web 11) and P5 (mcp 06), DEPENDENCY-AUDIT, root + backend/ai/mcp/web AGENTS + trackers, system-map, stack-contract, integration-contracts, api-surface, auth-contract (PLANNED section), research/ai-integration + ai-features-research.

### Changed
- **AI superpowers boundaries codified:** reports/audits ride a NEW scoped capability `CreateReport` (backend 24) — never a loosened OBO grant; **auth/OTP is human-only forever** (specs 23, ai 06/07/08, mcp 06).


## [Unreleased] — 2026-09-11 (Backend spec 09: AI service token + OBO principal + webhook HMAC)

### Added
- **Backend spec 09 implemented** on `feature/backend/09-ai-service-token-and-webhooks`:
  - `ServiceTokenHandler` (scheme `ServiceToken`, AI OBO): validates `Authorization: Bearer {GRIOT_SERVICE_TOKEN}` + `X-On-Behalf-Of: {Guid}` → looks up REAL USER in Users table → issues ClaimsPrincipal with the user's own identity (NameIdentifier = user.Id, Name = DisplayName, Email = Email, role `ai-on-behalf-of`, 4 scopes: ReadWorkspace · CreateTask · AddComment · CreateNotification; constant-time token comparison; per-call `IServiceScopeFactory` for user repo resolution.
  - `MultiAuth` policy scheme with `ForwardDefaultSelector` (Program.cs L200-213): JWT-shaped bearer → JwtBearer; anything else → ServiceToken; missing → JwtBearer so 401 challenges keep JWT shape.
  - `DomainControllerBase.ForbidIfAiCall()` / `IsAiCall` guard: checks role `ai-on-behalf-of` → returns 403 before any destructive endpoint.
  - GraphQL mirrors in GriotQuery/GriotMutation: IsAiCall on all DeleteWorkspace/DeleteProject/DeleteTask + RequireWorkspaceAdminOrOwnerAsync + RequireWorkspaceOwnerAsync.
  - `WebhookHmacMiddleware`: buffers body + HMAC-SHA256 compare against `WEBHOOK_SECRET`; runs BEFORE UseAuthentication; 401 on mismatch; downstream never sees forged payloads.
  - `TriggerDevClient` typed HttpClient + `TRIGGER_SECRET_KEY` Bearer auth for server→Trigger enqueue direction.
- **Unit tests:** `ServiceTokenHandlerTests` (9 tests, incl. OBO-header failure paths), `OboMembershipBoundaryTests` (pure IsMember), `AiAgentControllerGuardTests` (5 `ForbidIfAiCall` coverage)).

### Changed
- **Runtime bug fix:** `PATCH api/tasks/bulk-status` added AI guard at top of handler (TaskController.BulkStatusUpdate was the ONLY bulk-destructive endpoint without scope restriction).
- **Test file fixes:** `ServiceTokenAndWebhookTests.cs` fully rewritten for OBO; old virtual-ai-agent-GUID tests replaced/deleted (well-known fixed GUID virtual-member hack removed from runtime).
- **`.husky/pre-push` syntax bug fixed** (parenthesis).
- Hardcoded-GUID audit: 0 matches in application runtime code (seed/migrations/test fixtures are grandfathered).

### Fixed
- Pre-push SQL fixture: seed historical `Users` columns through parameterized SQL before applying `AddRefreshTokenFamilyId`, avoiding the premature `EmailVerified` insert. Extended the migration regression; verified 71 passing SQL-enabled tests, zero failures/skips, a clean build, and a healthy local API on 2026-09-10. No production schema or auth behavior changed in this repair.

## [Unreleased] — 2026-09-10 (Observability & audit system audit + hardening wave specs 18–22)

### Added
- **Five implementation-ready backend specs** (planning artifacts on this branch; each ships on its own feature branch):
  `18-search-filter-pagination-sorting.md`, `19-caching-and-rate-limiting.md`, `20-observability-logging-pipeline.md`,
  `21-database-resilience-backups-triggers.md`, `22-notification-fanout-email-inapp.md`.
- **`docs/observability/LOGGING-AUDIT-REPORT.md`** — source-level audit: `ApiLogs`/`ErrorLogs`/`AuditLogs`/`ActivityLogs` have entities, DbSets, indexes and role-gated read routes but **zero writers**; exception handler persists nothing; no DB triggers; no SQL Server backup chain; one global rate-limit window; no notification producer. Includes the incident answer matrix, per-layer sync table, and SQL query recipes.
- **`docs/decisions/ADR-004-observability-pipeline-and-db-resilience.md`** — decision record for the pipeline + triggers/backups + protection design and the ordering rationale.
- **`docs/observability/OBSERVABILITY-HARDENING-2026-09-10.md`** — the old-state vs new-state comparison: every concern (logging, error handling, forensics, triggers, backups, rate limits, caching, pagination, notifications, Postman, docs) before/after, why it changed, per-layer impact, and the cross-layer implementation flow.

### Changed
- **P0 reordered (canonical):** backend 09 → **20 → 18 → 19 → 22 → 21** → 11 → 10 — synced across `docs/planning/IMPLEMENTATION-ROADMAP.md`, `docs/DEPENDENCY-AUDIT.md`, root + backend `AGENTS.md`, backend progress tracker.
- `backend/project-kit/context/api-surface.md`: planned routes/preferences + spec-18 query contract pointers (labeled PLANNED).
- `backend/Postman/Griot.postman_collection.json`: new folder 14 "Observability & audit" (logs read guard tests + audit-trail assertion; strict assertions activate when specs 18–22 land).
- `docs/observability/MONITORING.md`: PLANNED-vs-implemented status labels so alert queries are not mistaken for live evidence.

## [Unreleased] — 2026-09-10 (Cross-system implementation roadmap + tracker completeness audit)

### Added
- **`docs/planning/IMPLEMENTATION-ROADMAP.md`** — canonical cross-system build order (P0 backend 09→11→10 → P1 web 01–09 → P2 ai 01–02→web 10→ai 03–05 → P3 mobile → P4 infra → P5 mcp → P6 qa) with per-phase rationale, the cross-system unlock edge list, and the web10↔ai02 circular-dependency resolution.
- **Missing progress trackers created** for `ai/`, `mcp/`, `mobile/`, `infra/` (`<system>/project-kit/context/progress-tracker.md`) — the contract-sync gate and git-branch-flow skill require one tracker per system; these systems previously had none.

### Changed
- Root `AGENTS.md` + all 7 system `AGENTS.md` files now carry build-order pointer sections ("Where This System Sits in the Build Order" / "Where We Are") citing the roadmap as the single source of truth for "which spec next".
- Root progress tracker: system status table rebuilt with roadmap phases; stale "infra 6 feature specs" fixed to 7; Next Steps point at P0 (backend 09 → 11 → 10) then the roadmap.
- Backend progress tracker: Next Steps re-sequenced **09 → 11 → 10** with unlock rationale (09 gates `ai/`+`mcp/`; 11 unblocks attachment UI; 10 freezes the API surface pre-Web).
- Web progress tracker: stale "no implementation until backend 04–06 live" removed; per-spec blockers added; web 10 explicitly deferred to P2 behind ai 01–02.
- QA progress tracker: per-spec blockers; qa 01–03 flagged as zero-code-dep gap fillers; qa 05 pinned to infra 05.
- `ai/project-kit/feature-specs/02-copilot-agent-streaming.md`: build-order note added — ai 02's "web 10" dependency is contract-design, not build-order; implementation order is ai 01 → ai 02 → web 10.
- `docs/DEPENDENCY-AUDIT.md`: header now delegates cross-system ordering to the roadmap.

## [Unreleased] — 2026-09-10 (Full REST surface specs 13–17 + Email-only communication)

### Added
- **Full REST implementation (specs 13–17)** on `feature/backend/08-api-testing-postman`: workspaces
  (+members/invites, invite lookup + accept), projects, boards (update/delete), columns (update/delete),
  tasks (CRUD, move, bulk-status via `usp_BulkUpdateTaskStatus`), comments (update/delete), attachments
  (create/delete metadata), notifications (unread-count, mark-read, read-all), dashboard summary,
  activity feed, error/audit logs — all behind `IDomainService`/`DomainService` with `IGenericRepository<T>`
  and `DomainControllerBase` (`sub` claim → Guid; `DomainError` → 400/401/403/404/409). Zero
  `Not implemented yet` remaining. 12 controllers = 54 routes, all mirrored 1:1 in the Postman collection.
- **Schema migration `20260910082854_AddTaskItemBoardId`**: `TaskItems.BoardId` (NOT NULL, backfilled
  from `Columns.BoardId` via join). Lazy-loading proxies enabled (`Microsoft.EntityFrameworkCore.Proxies`);
  all navigation properties marked `virtual`.
- **Communication layer (spec 12) final state = Email only**: multi-sender identities
  (`Brevo:Senders:<Key>` Security/Admin/NoReply/Support/Info/Team with per-profile reply-to; OTP=`security`,
  admin notice=`admin`) via `IEmailService`/`BrevoEmailService` — best-effort, never fails auth.
- **Fresh local DB** (`Griot` @ `localhost,14333`): dropped + recreated from the 4 migrations; stored
  procedures re-applied (`usp_BulkUpdateTaskStatus`, `usp_GetDashboardSummary` from
  `Griot.Infrastructure/Sql/`).

### Changed
- **Removed SMS/WhatsApp/Contacts/automations code + `ICommunicationService` facade** (user decision:
  only Email needed) — kept Brevo Email multi-sender identities. All comm docs, AGENTS (root + backend),
  stack-contract, integration-contracts, api-surface, README, AUTHENTICATION-GUIDE and
  COMMUNICATION-GUIDE now describe Email-only.
- `Program.cs`: `JsonStringEnumConverter` (enums serialize as strings), global exception handler with
  `X-Request-Id` on every response (incl. 500s), lazy-loading proxies, stable `TraceIdentifier`.
- `docs/api/auth-contract.md` config table, `api-surface.md` REST table + controller topology, and feature
  specs 13–17 updated to the implemented surface (`PUT/DELETE /api/boards/{id}`,
  `PUT/DELETE /api/tasks/{id}/comments/{commentId}`, `PATCH /api/notifications/{id}/read`,
  `GET /api/invites/{token}`, `POST /api/tasks/{id}/attachments`).

### Fixed
- `CS1998` async-without-await warnings in `WorkspaceService.cs` (scaffold kept for DI: now returns
  `Task.FromResult`).
- Lazy-loading proxy failure at design time: `ApiLog.User`, `AuditLog.ActivityLog`, `ErrorLog.User`,
  `ErrorLog.SolvedByUser` made `virtual`.

## [Unreleased] — 2026-09-08 (Feature 07 auth repair + Email-OTP 2FA)

### Added
- Email-OTP 2FA implemented end-to-end per `research/ai-features-research.md` §1: `POST /api/auth/otp/request` (202; also auto-sent with a branded `email_verify` code on register) + `POST /api/auth/otp/verify` (200/401/429); sets `Users.EmailVerified`; purposes `email_verify`/`login_2fa`/`password_reset`.
- Branded Brevo email templates (`Griot.Application/Email/BrandedEmailTemplate.cs` — C# port of the Griot branded shell, per-purpose copy) + Brevo transport (`Griot.Infrastructure/Email/BrevoEmailService.cs`;`Brevo:ApiKey`/`Brevo:FromEmail`/`Brevo:FromName`/`Brevo:ContactToEmail`; admin "New user registered" notice on register).
- **Email provider = Brevo** (replaced Resend): Resend sandbox restricted delivery to the account owner / verified-domain recipients; Brevo sends to arbitrary recipients on the free tier (300 emails/day) with a verified sender.
- `Users.EmailVerified` bit column (migration `20260908144212_AddRefreshTokenFamilyId` amended) + OTP challenge lifecycle in `AuthRepository` (hash-at-rest, 10-min expiry, 5-attempt lockout, supersede-old-challenges).
- OTP routes/statuses/config documented in `docs/api/auth-contract.md`, `backend/project-kit/context/api-surface.md`, integration contracts, AGENTS.md, and spec 07 acceptance criteria.
- Tests: unit OTP coverage in `AuthServiceTests` + SQL/HTTP `HttpOtp_VerifyConsumes_AndMarksEmailVerified_AndLocksAfterFive` (optional `GRIOT_RUN_SQL_TESTS=1`).

### Fixed
- Root "Not implemented yet" auth responses: caused by a stale pre-auth build in `backend/src/Griot.Api/bin`; the implemented auth controller/service/repository now serve 201/200/401/204 — rebuild required. README scaffold note unchanged (workspaces stub still legit(.
- CodeRabbit review (8 actionable comments(: verified already-applied code fixes in the working tree (singleton `IConnectionMultiplexer`; `localhost:6380` fallback; `64-hex` token validation; timing-safe dummy-password login; `DbUpdateException`→`DuplicateEmailException` mapping; atomic family-scoped rotation with transactional conditional-update; family-scoped reuse revocation) and completed the remaining doc/stale-status synchronization (GRAPHQL-STATUS zero-warning/, DEPENDENCY-AUDIT Spec  ‏08 next(.

- Applied CodeRabbit review batch (7 actionable findings): admin-recipient selection ignores blank/whitespace config values before `CanonicalAdminInbox`; ADR-003 D12 corrected to the 3-key `IConfiguration` bearer chain (`Brevo:ApiKey` → `BREVO_API_KEY` → `Brevo__ApiKey`); IMPLEMENTATION-CHECKLIST wording corrected to host-startup options validation; RUNBOOK-ROLLBACK PITR prerequisites (enabled + first base backup), quiesce-before-cutover, and PostgreSQL 16 `recovery.signal` guidance; recovery contract (`POSTGRES_RECOVERY_TARGET_TIME`, `<service>-restored-YYYYMMDD-HHMM`, read-only WAL, cutover) synced into infra spec 06, integration contracts, deployment docs, root + infra AGENTS.md. Rebranded Resend to Griot: canonical admin inbox `info.donartkins.ke@gmail.com`, email footer brand "Griot", `SITE_URL` default `https://griot.app`, doc examples use `alice@example.com`.

## [0.2.0] - 2026-09-03 (planning phase)

### Added
- Seven-system monorepo kits: `backend/`, `web/`, `mobile/`, `infra/`, `qa/`, `ai/`, `mcp/` — each with `AGENTS.md`, `.agents/skills/`, and `project-kit/` (context + feature-specs + diagrams + examples).
- Root `AGENTS.md` orchestrator + shared `.agents/skills/` (contract-sync, figma-make-erd, git-branch-flow, throttling-prevention).
- `PROMPTS/` week-grouped prompts, incl. the Figma Make ERD **single extensive master prompt (no length limit)**.
- Schema expanded to **16 tables** (13 core + `ApiLogs` + `ErrorLogs` + `AuditLogs`) + 5 enums — observability/audit decided pre-implementation.
- Extensive system-design diagram specs (C4 Level 1/2, API component, sequence ×3, task state machine, deployment, auth matrix, API surface map, AI system context).
- `docs/` suite: architecture, NFR + capacity plan + risk register + runbook + change management, database design, ADRs, SEO/deployment, monitoring.

### Changed
- Root `project-kit` consolidated to cross-system orchestration docs (system-map, stack-contract, integration-contracts, UI docs, progress-tracker).
- Branch model formalized: `feature/<system>/<NN>-<slug>` (group-aware, one spec = one branch).

## [0.1.0] - 2026-09-02

### Added
- Bootcamp research corpus (`research/`) and initial planning kit.

---

v0.1.0 → v0.2.0: planning/dot-dash only. No production code yet.
