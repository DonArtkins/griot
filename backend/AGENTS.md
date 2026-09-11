# AGENTS.md — Griot Backend / API (ASP.NET Core 8)

## Read This First

You are the agent for the **Backend / API system** of Griot. This system is the sole owner of data and business logic. Everything else (web, mobile, AI, MCP) talks to this system and nothing else touches SQL Server.

Stack (exact per `project-kit/context/stack-contract.md`): .NET 8, ASP.NET Core Web API, EF Core 8, Dapper 2.x, HotChocolate GraphQL 14+, SQL Server 2022 (primary), PostgreSQL 16 (secondary), Redis 7. Auth is `[own-stack]`: custom JWT + Argon2 + rotated refresh tokens + Brevo Email OTP 2FA. Communication is `[own-stack]`: Brevo transactional Email only (single verified sender — 2026-09-11: `Brevo:Senders:*` profile map removed) — see `docs/communication/COMMUNICATION-GUIDE.md` + spec 12. Reports [own-stack]: RBAC-scoped scheduled & ad-hoc.

## Solution Layout (Separation of Concerns)

```
backend/
├── Griot.sln
├── global.json                  # pins SDK 8.0
├── src/
│   ├── Griot.Api/               # hosting only: Program.cs, controllers, GraphQL, middleware
│   ├── Griot.Application/       # services only: business rules, orchestration
│   ├── Griot.Domain/            # entities + enums; ZERO references
│   └── Griot.Infrastructure/    # GriotDbContext, EF repos, Dapper repos, Redis, migrations, Sql/
└── tests/Griot.Tests/           # xUnit + WebApplicationFactory
```

**Rules:** controllers/resolvers are thin (no business logic). `Griot.Application` has no EF/HTTP/Redis. `Griot.Infrastructure` is persistence only. `Griot.Domain` depends on nothing.

## Reading Order

1. Root `AGENTS.md` + `project-kit/context/{system-map,stack-contract,integration-contracts}.md`.
2. `research/week-02-backend-api-development.md` (+ `research/gtp-2026-prep.md` §6.5).
3. `backend/project-kit/context/architecture.md` → `data-layer.md` → `api-surface.md` → `code-standards.md`.
4. The approved ERD at `diagrams/erd/` (entity/enum names are a contract).
5. The current feature spec (one at a time, numeric order).

## Required Skills

Check `/.agents/skills/` (contract-sync, figma-make-erd, git-branch-flow, throttling-prevention) and `backend/.agents/skills/` (dotnet-ef-core, sql-server-2022, dapper-stored-procs, hotchocolate-graphql, jwt-argon2-auth) and follow the relevant `SKILL.md` exactly.

## Where This System Sits in the Build Order (canonical: `docs/planning/IMPLEMENTATION-ROADMAP.md`)

**Phase P0 — current.** Backend specs 01–09, 12 (Email-only), 13–17 are ✅ — 09 (AI service token + webhooks) is delivered OBO. Remaining order: **20** (observability pipeline — fills ApiLogs/ErrorLogs/AuditLogs/ActivityLogs; audit found zero writers) → **18** (search/filter/pagination/sorting) → **19** (caching + rate-limit partitions) → **22** (notification fan-out in-app + email) → **21** (DB audit triggers + backup chain + restore drill) → **23** (critical-action OTP & step-up: login 2FA, forgot/reset password, delete account, guarded-op step-up) → **11** (blob storage) → **28** (project lifecycle report evidence) → **24** (AI reports & export surface: Report rows, PDF/CSV artifacts, `audit-summary`, 5th OBO scope `CreateReport`) → **25** (role-tiered log access + AI capability gateway) → **26** (AI memory & conversations) → **27** (incident alerting + confirmed SuperAdmin broadcasts) → **10** (API docs — freezes the hardened surface before Web consumes it). Brevo: verified senders need a domain YOU own (`griot.vercel.app` is Vercel-owned and cannot be authenticated — COMMUNICATION-GUIDE §7b). Logging runtime explained in `docs/observability/HOW-LOGGING-WORKS.md`. Rationale: `docs/observability/LOGGING-AUDIT-REPORT.md` + ADR-004. When 10 lands, P0 closes and the **Web system (P1)** becomes the active layer. Track state in `backend/project-kit/context/progress-tracker.md`; never reorder without updating the roadmap + `docs/DEPENDENCY-AUDIT.md` in the same branch.

## Verification Gates

- `dotnet build` clean; `dotnet test` green (xUnit, incl. refresh-rotation replay + bulk-update atomicity).
- `docker compose up` runs api + sqlserver + postgres + redis; `/health` answers.
- Postman collection runs end-to-end (Newman in qa/infra CI).
- No hardcoded secrets; `.env.example` current; contracts synchronized.

## Hard Rules

1. Schema changes go through the ERD first (figma-make-erd skill), then a migration.
2. Both REST and GraphQL share `Griot.Application` services — zero drift allowed.
3. `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of: {Guid}` resolves to the restricted **real-user OBO** principal ONLY when an **unexpired `ServiceToken:Delegations:{userId}` grant** (UTC `ExpiresAtUtc`, `WorkspaceIds`, `Scopes`) binds the service identity to that user — allowed workspaces and narrowed scopes are derived from that grant, never from client headers alone. Role is `ai-on-behalf-of`, scope vocabulary ReadWorkspace/CreateTask/AddComment/CreateNotification (+ `CreateReport` PLANNED spec 24); `POST /api/webhooks/trigger` HMAC is verified by middleware (`Webhook:Secret` ?? `WEBHOOK_SECRET`) before auth; `TriggerDevClient` enqueues after persist (`Trigger:SecretKey` ?? `TRIGGER_SECRET_KEY`, never throws/rolls back).
4. Every raw SQL / Dapper call is parameterized; stored procs are `usp_` prefixed and idempotent.
5. Auth details (Argon2, 15-min JWT, refresh rotation with revocation-on-reuse) match spec 07 exactly.

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**

## Implemented authentication contract (Feature 07)

Use the [auth contract](../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Email-OTP 2FA implemented: `POST /api/auth/otp/request` (202; `email_verify` auto-sent on register) + `POST /api/auth/otp/verify` (200/401/429); sets `Users.EmailVerified`; branded Brevo template per purpose. Registration returns 201 after SQL persistence; malformed refresh returns 401 and authenticated logout remains 204.

## Implemented communication contract (Spec 12 — own-stack)

Outbound messaging is **Email only** (SMS/WhatsApp/Contacts/automations removed from code
in this branch). Every transactional email goes through `IEmailService`
(`BrevoEmailService`, best-effort, non-throwing) with per-purpose sender identities
(`Brevo:Senders:<Key>`, reply-to per profile). OTP uses `security`, the admin new-user
notice uses `admin`. Best-effort is scoped to **registration only** (register always
returns 201; a Brevo failure there is logged, not surfaced). `/api/auth/otp/request`
is the one caller that surfaces delivery failure: Brevo reject/outage → **HTTP 502**
(202 on success; 401 unknown email; 429 rate-limited). Redis gate:
`ratelimit:otp:request:{email}` 3/15min before Brevo is called. Brevo's 300/day free
cap is a real operational budget the OTP window slows but does not make unreachable.
Full contract: `docs/communication/COMMUNICATION-GUIDE.md`; owner spec:
`feature-specs/12-communication-channels-brevo.md`.

## Implemented AI boundary contract (Feature 09 — own-stack)

AI authenticates with Bearer GRIOT_SERVICE_TOKEN, trusted X-On-Behalf-Of and an
unexpired backend-configured delegation under ServiceToken:Delegations:{userId}
(WorkspaceIds, Scopes, ExpiresAtUtc). Grants use only ReadWorkspace/CreateTask/
AddComment/CreateNotification and may narrow that set; membership is checked again.
MVC AiAccessFilter and GraphQL AiFieldMiddleware default-deny unmapped operations.
Auth/OTP, updates, deletes, invites, member management and raw-log reads are closed
to AI. CreateNotification has no generic public creation route yet. GraphQL denials
are errors with HTTP 200 for application/json; REST denials are 403.

MultiAuth compares the configured token first, then selects the appropriate validator;
blank primary secrets allow environment fallback. HMAC callbacks have a fixed 64 KiB
byte cap and return 503 until durable job-bound replay-safe dispatch ships in backend 20
(401 bad HMAC, 413 oversized). TriggerDevClient validates HTTPS and disables redirects;
it sends the Trigger payload envelope but has no production callers or durable recovery.
Do not wire it before spec 20's outbox. Contract: docs/api/ai-service-token-contract.md.

Before committing or pushing implementation, run `python3 scripts/check-contract-sync.py` from
the repository root. Synchronize the owning spec, dependent specs, planning,
research, docs, contexts, agent instructions, diagram sources and progress notes
in the feature branch. Planned behavior must be labeled and must not count as
implemented acceptance evidence. Run the system verification gates as well.

## Multi-Tenant Migration Wave — 2026-09-11 [own-stack] (PLANNED)

Backend owns the tenant foundation: specs **29–35** (29 Organizations schema + Pool-model isolation via `ITenantContext` + EF global query filters; 30 JWT v2 claims `name/org/role/perms` + `POST /api/auth/select-organization` + SuperAdmin bootstrap + `JWT__Key` ≥512-bit policy — refresh tokens stay opaque, never JWTs; 31 RBAC: system roles Owner/Admin/ProjectManager/Member/Client + Admin/PM-created custom roles from a fixed permission catalogue; 32 SuperAdmin company onboarding/suspend/reactivate; 33 offboarding export → 30-day retention → purge; 34 client portal + `ClientFeedback` + AI client boundary; 35 handoff + AI-generated client manual + client offboarding + maintenance) plus revision specs **36–51** for every *implemented* backend feature (36→01 … 51→20 — the implemented spec files themselves stay frozen). Spec 29 requires the Figma-Make-approved ERD amendment `diagrams/erd/multi-tenant-amendment.md` first (hard rule 4). Canonical contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`. Includes the workspace POST fix: `CreateWorkspaceAsync` must hydrate member user data (`displayName`/`email`/`avatarUrl`) so POST responses match GET.

## Audit synchronization — 2026-09-11

Current implementation remains backend 09 review hardening; next is backend 20 after review. Future planning is not completed implementation. P0: backend 09 → 20 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10. P2: ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12. Full requirement/review ledger: `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
