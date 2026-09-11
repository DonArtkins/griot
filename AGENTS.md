# AGENTS.md — Griot Monorepo Orchestrator (GTP 2026 Bootcamp)

## Current working-tree checkpoint — 2026-09-11

Backend 29 (multi-tenant foundation) is IMPLEMENTED on
`feature/backend/29-multi-tenant-foundation-organizations` — commits `9447e15`, `9f7400e`
(fail-closed `ITenantContext` scoping, `OrganizationId` on tenant tables + observability
stamps, migration `20260911190926` with all Organization FKs ON DELETE NO ACTION, ERD
Amendment v2; build 0W/0E, 152 tests passed / 8 SQL-skipped; the database was rebuilt
from all migrations and is verified up to date on the user's machine, 2026-09-11).
Pushed after explicit user approval (2026-09-11). Outstanding gate before backend 30:
tenancy ERD approval (`diagrams/erd/multi-tenant-amendment.md`) only — the
migration-apply gate is CLOSED. Roadmap §P0.5 governs: 29 → 30 → 31 → 32 → 33 →
34 → 35, then the hardening sequence. This checkpoint supersedes older
next-feature statements below. Read the [preflight and completion plan](docs/planning/BACKEND-29-PREFLIGHT-2026-09-11.md)
and the backend tracker before proceeding.

## Read This First

You are an AI agent working on **Griot** (*GREE-oh*), a project-management web app built for the **Sababisha Solutions GTP 2026 Bootcamp**. Griot is the "accurate, shared record of what happened and what's next" — workspaces, projects, boards, tasks, comments, notifications, and an AI copilot.

The bootcamp defines the systems; Griot runs exactly on them. **`research/GTP 2026 BOOTCAMP EDITION.pdf` (extracted to `research/_bootcamp_2026.txt`) is the contract.** It defines **five official systems** — Backend/API (Week 2), Web (Week 3), Mobile (Week 4), DevOps (Week 5), Quality Engineering (Weeks 6–7) — and `research/ai-integration.md` adds the **[own-stack] AI system** (Trigger.dev agents + MCP server + Copilot).

**Every system is a self-contained worktree with its own kit.** Do not treat this repo as one app.

## The Seven Systems

| # | System | Folder | Weekly source | Entry point |
|---|---|---|---|---|
| 1 | Backend / API (.NET 8 + SQL Server) | `backend/` | Week 2 | `backend/AGENTS.md` |
| 2 | Web (React 18 + Vite + MUI) | `web/` | Week 3 | `web/AGENTS.md` |
| 3 | Mobile (Flutter 3.19+) | `mobile/` | Week 4 | `mobile/AGENTS.md` |
| 4 | DevOps / Infra (Docker, Vercel, Railway, CI/CD) | `infra/` | Week 5 | `infra/AGENTS.md` |
| 5 | Quality Engineering (tests, OWASP, k6) | `qa/` | Weeks 6–7 | `qa/AGENTS.md` |
| 6 | AI agents (Trigger.dev v4) [own-stack] | `ai/` | `ai-integration.md` | `ai/AGENTS.md` |
| 7 | MCP server [own-stack] | `mcp/` | `ai-integration.md` | `mcp/AGENTS.md` |

## Mandatory Reading Order

1. `research/_bootcamp_2026.txt` + `research/gtp-2026-prep.md` — the contract and environment rules.
2. `research/week-0X-*.md` for the week whose system you are working in.
3. `research/ai-integration.md` for the AI/MCP systems.
4. `project-kit/context/system-map.md` — how the systems communicate.
5. `project-kit/context/stack-contract.md` — the exact stack + [own-stack] markers.
6. `project-kit/context/integration-contracts.md` — cross-system contracts (ports, env, API, GraphQL).
7. The **system's own** `AGENTS.md`, then its `project-kit/context/*`, then its `project-kit/feature-specs/*`.
8. `PROMPTS/` for the Figma Make / FigJam / agent prompts that produce each system's designs (ERD before schema, wireframes before UI).
9. `diagrams/**` for approved Figma Make/FigJam artifacts that govern implementation.

## Where We Are — Implementation Order (canonical: `docs/planning/IMPLEMENTATION-ROADMAP.md`)

The cross-system build order is **P0 backend 09 → 20 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10 → P1 web 01–09 → P2 ai 01–02, web 10, ai 03–12 → P3 mobile 01–07 → P4 infra 01–07 → P5 mcp 01–06 → P6 qa 01–13**. Right now: backend specs 01–09, 12 (Email-only), 13–17, 20 (observability pipeline) and 29 (multi-tenant foundation, 2026-09-11 — pending its ERD-approval + migration-apply gates) are ✅ — **the next spec is backend 30 (Auth & JWT v2)** per roadmap §P0.5, which supersedes the older post-20 hardening order below (**20** logging pipeline → **18** search/filter/pagination → **19** cache/rate-limits → **22** notification fan-out → **21** DB triggers/backups → **23** critical-action OTP/step-up → **11** blob storage → **28** project lifecycle report evidence → **24** AI reports & export surface (`CreateReport` scope) → **25** role-tiered log access + AI capability gateway → **26** AI memory & conversations → **27** incident alerting + confirmed SuperAdmin broadcasts; rationale: `docs/observability/LOGGING-AUDIT-REPORT.md` + ADR-004). Every system's `AGENTS.md` carries a "Where This System Sits in the Build Order" section, and every system has a `project-kit/context/progress-tracker.md`. Do not pick a "next feature" from anywhere else — the roadmap + the owning system's tracker are the single source of truth, and any reorder must update the roadmap + `docs/DEPENDENCY-AUDIT.md` + affected specs in the same branch.

## Required Skills

**CRITICAL DIRECTIVE: check `.agents/skills/` and the current system's `.agents/skills/` before any implementation.**

- Root shared skills live in `/.agents/skills/` (contract sync, Figma Make ERD, git branch flow, throttling prevention).
- System skills live in `<system>/.agents/skills/` (dotnet-ef-core, hotchocolate, mui, riverpod, trigger-dev, newman, k6…).
- Read the relevant `SKILL.md` and follow it exactly. Do not rely on memory for current APIs.

## Cross-System Rules (Hard Rules)

0. **Contract synchronization is a hard gate.** A change to any cross-system contract (API route, GraphQL type, env var, port, entity/enum, auth token shape, `GRIOT_SERVICE_TOKEN`, Docker service name, MCP tool id) must be reflected in the owning system's feature spec, all dependent systems' specs, the relevant context files, the root `AGENTS.md`, and `docs/` in the same branch. Never leave a system describing a stale contract.
0a. **Optimization phases are implementation gates.** Production-blocking optimizations (Phase 1: blob storage, Netdata monitoring, dashboard caching, GraphQL DataLoader, pagination caps) ship before public launch. Post-baseline optimizations (Phase 2: indexes, Redis caching, read replicas) apply only after k6 evidence proves p95 latency targets are missed. Post-bootcamp enhancements (Phase 3: R2 migration, Prometheus, offline queue, code splitting) are deferred until cost/scale justifies them. See `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md` for full roadmap.
1. **Separation of concerns is physical.** `backend/` owns data + API; `web/` + `mobile/` own presentation; `ai/` + `mcp/` own intelligence; `infra/` owns containers + deployment; `qa/` owns test lifecycles. No system writes code into another system's folder.
2. **AI never writes to SQL Server directly.** Every AI read/write goes through the .NET API via `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of: {Guid}` (resolved to a real-user **On-Behalf-Of** principal — role `ai-on-behalf-of`, exactly four scopes: ReadWorkspace/CreateTask/AddComment/CreateNotification; no deletes, no invites, no member management; same policy code as any member). **PLANNED (backend 24):** a FIFTH scope `CreateReport` — a new, narrow, auditable capability for the report/audit tools (ai 06/07, mcp 06) only; auth/OTP (spec 23), deletes, invites and member management stay permanently closed to AI.)
3. **The PDF stack is never substituted silently.** Every deviation must carry `[own-stack]` and a written rationale in `project-kit/context/stack-contract.md`.
4. **ERD before schema, wireframes before UI.** The approved Figma Make ERD (`diagrams/erd/`) is the only source for entity/enum names; no schema code may exist before it is approved.
5. **Planning before implementation.** Present a concrete plan and wait for explicit approval before schema migrations, API surface changes, Docker/Compose changes, deployment changes, or writing any production code. **The system-design docs + diagrams must be complete and approved before implementation starts.**
6. **One feature spec at a time, one feature branch = one PR (group-aware).** Branches are `feature/<system>/<NN>-<slug>` (e.g. `feature/backend/02-sql-server-efcore`, `feature/web/05-secure-auth`). Never batch specs, never commit progress-tracker updates directly to `main`, never commit code to `main` directly. **Wait for the user's approval after committing to GitHub, and update the progress tracker before pushing to GitHub. When starting the next spec, switch to its feature branch so each feature has its own branch; any update being done to a feature must be pushed to that feature's branch, contract sync run, and push only when all hard gates pass.** (Full text: see footer of this file, every AGENTS.md, every progress tracker, every context file, and every feature spec.)
7. **Throttling prevention.** See `.agents/skills/throttling-prevention/SKILL.md`. Batch reads/writes, prefer shell for bulk ops, pause on throttling.
8. **Every error/fix is tested + documented.** If the AI agent hits an error and fixes it, the fix must be tested to work, then documented (spec + context + progress-tracker + docs) — it's no longer what the spec said. See `docs/planning/CHANGE-MANAGEMENT.md`.
9. **Skills + inspo + docs are mandatory.** Read `.agents/skills/` + `inspo/` + `docs/` before building UI/features. Agents MUST use the `inspo/` + `examples/` folders for UI quality (Foundrie pattern).
10. **Each feature spec is implementation-ready.** A spec must contain Setup/Initialization, Separation of Concerns, Docker & Deploy, and explicit acceptance criteria — so implementation is straight-line. No vague specs.

## Verification Gates (per system)

- **backend**: `dotnet build` + `dotnet test` green; compose local stack healthy.
- **web**: `npm run lint && npm run typecheck && npm test && npm run build` green.
- **mobile**: `flutter analyze && flutter test` green.
- **ai/mcp**: `npm run lint && npm run typecheck && npm test` green (golden transcripts, MCP contract tests — no LLM in CI).
- **infra**: `docker compose up` reproduces the full local topology; CI jobs green.
- **qa**: xUnit, Jest+RTL, Flutter, Cypress, Newman green; coverage ≥80% (service layer + auth); k6 baseline recorded; OWASP logged.

## Stack at a Glance

Backend: .NET 8, ASP.NET Core Web API, EF Core 8, Dapper 2.x, HotChocolate 14+, SQL Server 2022, PostgreSQL 16. Web: React 18.3, Vite 5, MUI v6, Apollo, Axios, TanStack Query 5. Mobile: Flutter 3.19+, Dart 3, GraphQL Flutter, Riverpod. DevOps: Docker 26+, Compose v2, Vercel, GitHub Actions, Railway/Render/Azure. Auth [own-stack]: JWT + Argon2 + Redis. Communication [own-stack]: Brevo transactional Email only — multi-sender identities (`Brevo:Senders:<Key>`, reply-to per profile) — see `docs/communication/COMMUNICATION-GUIDE.md`. Reports [own-stack]: RBAC-scoped digests & ad-hoc. AI [own-stack]: Trigger.dev v4 Level-4 Autonomous Agents, MCP — orchestrated by the .NET backend only (Trigger.dev = compute adapter, never a data owner; web/mobile never trigger or poll Trigger.dev, web may consume the scoped, read-only Copilot stream; contract: `research/ai-integration.md` §2a).

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**

## Implemented authentication contract (Feature 07)

Use the [auth contract](docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Email-OTP 2FA implemented: `POST /api/auth/otp/request` (202; `email_verify` auto-sent on register) + `POST /api/auth/otp/verify` (200/401/429); sets `Users.EmailVerified`; branded Brevo template per purpose. Registration returns 201 after SQL persistence; malformed refresh returns 401 and authenticated logout remains 204.

## Implemented communication contract (Spec 12 — own-stack)

Outbound operator/user email is **Email only** (SMS/WhatsApp/Contacts/automations were
removed from code in the same branch; contract-synced docs below). Every transactional
email goes through `IEmailService` (`BrevoEmailService`, best-effort, never throws) with a
single Brevo-verified sender identity (`Brevo:FromEmail`/`BREVO_FROM_EMAIL` — required,
dashboard-verified; `Brevo:FromName` default `Griot`; optional per-call `message.ReplyTo`;
2026-09-11 user decision: the `Brevo:Senders:<Key>` profile map — Security/Admin/NoReply/
Support/Info/Team, e.g. `noreply@griot.app` — was removed because `griot.app` is unverified;
every email sends from the one dashboard-verified sender).
Callers today: register (OTP `email_verify` to user + admin "New user registered" notice
to `Brevo:ContactToEmail`) and `/api/auth/otp/request`. The OTP request route itself is
rate-limited (3/15min/email via Redis) before Brevo, and the API global limiter caps
100/min/caller — Brevo's 300/day free cap is never reachable from app code.
Full contract: `docs/communication/COMMUNICATION-GUIDE.md`; owner spec:
`backend/project-kit/feature-specs/12-communication-channels-brevo.md`.

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

## AI superpowers & critical-action OTP — 2026-09-11 planning wave [own-stack]

User-approved planning (no production code yet — all PLANNED) creating backend specs **23** (critical-action OTP/step-up: login 2FA enforcement, forgot/reset password, delete account, guarded-op step-up), **24** (AI reports & export surface: `Report` rows, PDF/CSV artifacts via blob, `audit-summary`, 5th OBO scope `CreateReport`), **25** (role-tiered log access + AI capability gateway — per-OBO-role tool manifest, raw logs SuperAdmin/Dev only), **26** (AI memory & conversation surface), **27** (incident alerting + confirmed SuperAdmin broadcasts); AI specs **06–10** (knowledge+system auditor · reports PDF+CSV · Level-4 executor · agentic BI copilot + memory + capabilities manifest · SuperAdmin ops agent); web specs **11** (Reports & Audit Center) and **12** (dedicated AI Workspace sidebar); mcp spec **06** (v2 report/audit tools, role-aware). Research: `research/ai-features-research.md` / `research/ai-integration.md`. Brevo: senders fail until a domain YOU own is verified — `griot.vercel.app` cannot be authenticated (Vercel-owned), see `docs/communication/COMMUNICATION-GUIDE.md` §7b. Logging runtime: `docs/observability/HOW-LOGGING-WORKS.md` (owner: spec 20).

## Database rollback / recovery contract

Railway managed PostgreSQL PITR recovery provisions an independent restored service named
`<service>-restored-YYYYMMDD-HHMM`; `POSTGRES_RECOVERY_TARGET_TIME` is set automatically on it and the
restored service replays the source WAL archive read-only up to the target time. Validate
(`AuditLogs`/`ActivityLogs` timestamp + smoke tests), quiesce dependent-service writes (maintenance/
read-only) or reconcile post-target writes, THEN switch `ConnectionStrings__Default` to the restored
service and redeploy (Vercel/Trigger env in the same step). This cutover flow is DISTINCT from normal
SQL Server `ConnectionStrings__Default` configuration. PITR must be enabled before an incident with its
first post-enable base backup complete, or no historical restore window exists. Full runbook:
`docs/planning/RUNBOOK-ROLLBACK.md`; owning spec: infra spec 06.

Before committing or pushing implementation, run `python3 scripts/check-contract-sync.py` from
the repository root. Synchronize the owning spec, dependent specs, planning,
research, docs, contexts, agent instructions, diagram sources and progress notes
in the feature branch. Planned behavior must be labeled and must not count as
implemented acceptance evidence. Run the system verification gates as well.

## Multi-Tenant Migration Wave — 2026-09-11 [own-stack] (PLANNED)

User-directed planning wave (research: `research/LYNCXS-MULTI-TENANT-SYSTEMS-ENGINEERING.md`; canonical contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`; planned ERD amendment: `diagrams/erd/multi-tenant-amendment.md`). Griot becomes a **multi-tenant platform**: the operator (**SuperAdmin** — you, owner of Griot) onboards **Companies (`Organizations`)**, each company has an **Admin (owner)** who sees ALL projects **inside that company only**, ProjectManagers manage their projects, and **Clients** get a satisfaction-first portal (progress view + feedback + handoff + maintenance). Pool tenancy model; `OrganizationId` on every tenant table; JWT v2 access tokens carry `name`/`org`/`role`/`perms` while **refresh tokens stay opaque by design** (they are not JWTs — jwt.io decoding them blank is correct); production `JWT__Key` ≥ 512 bits, secret-store only.

**Spec-numbering rule of this wave:** implemented backend specs are **frozen** — their multi-tenant behavior ships via NEW revision specs **36–51** (36→01 … 51→20); unimplemented backend specs are bumped in place; NEW multi-tenant specs are **29–35**. Other layers: all existing specs bumped + **web 13–16, mobile 08–09, ai 13–15, mcp 07, qa 14** added. Order after backend 20: **29 → 30 → 31 → 32 → 33 → 34 → 35**, then the existing hardening order, revisions landing with the branches that touch their base features, **10** last. JWT skills installed for agents: `.agents/skills/jwt-{decode,encode,validate}/` (`npx skills add jsonwebtoken/jwt-skills`). Roadmap: `docs/planning/IMPLEMENTATION-ROADMAP.md` §P0.5; dependency edges: `docs/DEPENDENCY-AUDIT.md`.

## Audit synchronization — 2026-09-11

Implemented through backend 20 (observability pipeline); backend 29 (multi-tenant foundation) implemented 2026-09-11 on `feature/backend/29-multi-tenant-foundation-organizations` — roadmap §P0.5 next is backend 30 after 29 completes its outstanding gates. Future planning is not completed implementation. P0 (2026-09-11): backend 29 ✅ → 30 → 31 → 32 → 33 → 34 → 35 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10. P2: ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12. Full requirement/review ledger: `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`.

## Review-batch exception — 2026-09-11

The user authorized committing and pushing the reviewed planning/contract and AI dependency corrections together on the backend 09 branch, in response to the one-time grouping exception request. Scope and validation: `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`. Future production features retain one spec per branch/PR; merge and next-feature work remain subject to user approval.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
