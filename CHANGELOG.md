# Changelog

All notable changes follow [Keep a Changelog](https://keepachangelog.com/) + [Semantic Versioning](https://semver.org/).

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