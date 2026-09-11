# AGENTS.md - Griot AI Agents (Trigger.dev v4) [own-stack]

## Read This First

You are the agent for the **AI system** of Griot (the [own-stack] extension in `research/ai-integration.md`). You build the orchestration that wraps the bootcamp backend - agents, scheduled workflows, and the Copilot conversation engine. You never replace the backend, never touch SQL Server, and never hold database credentials.

Stack: Trigger.dev v4 (Node 20, own lockfile), `@trigger.dev/sdk`, LLM SDK (Anthropic/OpenAI). Web integration via `@trigger.dev/react-hooks`.

## Reading Order

1. Root `AGENTS.md` + root `integration-contracts.md` (AI tool contract).
2. `research/ai-integration.md`.
3. `ai/project-kit/context/{architecture,roster,security}.md`.
4. Current spec.

## Required Skills

Root shared skills + `ai/.agents/skills/` (`trigger-dev-tasks`, `ai-agent-security`).

## Where This System Sits in the Build Order (canonical: `docs/planning/IMPLEMENTATION-ROADMAP.md`)

**Phase P2 — AI hop.** Backend **09** prerequisite is met (`GRIOT_SERVICE_TOKEN` plus `X-On-Behalf-Of` and HMAC webhook — the only legal data path). Starts after P1 (web 01–09); AI implementation remains planned. Own order: **01 → 02 → [web 10] → 03 → 04 → 05 → 06 → 07 → [web 11] → 08 → 09 → [web 12] → 10 → 11 → 12**: the superpowers wave — 06 knowledge agent + system auditor, 07 report generation (PDF + CSV), 08 advanced Level-4 executor, 09 agentic BI copilot + capabilities manifest + memory (needs backend 25/26), 10 SuperAdmin ops agent (needs backend 27); web 11 (Reports & Audit Center) sits between ai 07 and 08; web 12 (dedicated AI Workspace sidebar) after ai 09. web 10 sits inside this phase because it consumes ai 02's realtime stream, while ai 04's propose-before-write wraps web 10's approval cards. ai 02's "web 10" dependency is contract-design, not build-order. NEVER: OTP/auth/delete/invite/member tools for AI (spec 23 human-only). Entry branch: `feature/ai/01-trigger-setup`. Track state in `ai/project-kit/context/progress-tracker.md`.

## Verification Gates

- `npm run lint && npm run typecheck && npm test` green (golden transcripts with a MOCKED LLM - no network in CI).
- Scheduled agents run in dev. Copilot streams; approval cards write via REST.
- Cost caps enforced; audit log rows present for every tool call.

## Hard Rules

1. AI writes only via the backend GraphQL with `GRIOT_SERVICE_TOKEN` plus `X-On-Behalf-Of: {real User.Id}`. Service-token REST write-back uses the same headers. Scheduled runs must also resolve an authorized real-user identity; never invent a synthetic user. Role `ai-on-behalf-of` carries exactly ReadWorkspace/CreateTask/AddComment/CreateNotification; no deletes/invites/member management.
2. Mutations are proposed -> approved -> executed by the app, never by the agent.
3. LLM keys only in `ai/.env`; never in web or backend.
4. **Trigger.dev is a compute/orchestration adapter, never a data owner.** Every result is written back by calling the .NET API (`POST /api/webhooks/trigger` HMAC or service-token REST); `ai/` never writes to SQL Server, never holds DB credentials.
5. **`ai/` tasks are triggered only by the .NET backend or by a Trigger.dev schedule** (backend side: Trigger.dev REST/SDK, server-to-server `TRIGGER_SECRET_KEY`). Web/mobile never trigger or poll Trigger.dev — they call the .NET API, which enqueues tasks. The only direct web↔Trigger channel is the Copilot realtime stream (scoped access token, read-only delivery).
6. **Scheduled agents are the ONE explicit exception to "the backend is the only trigger."** `dueReminders`, `sprintDigest`, `staleBoard`, `standupBuilder` are started by Trigger.dev's own cron scheduler (no user request exists to originate them), NOT by the .NET backend. They are still not data owners: every scheduled run reads through backend GraphQL with `GRIOT_SERVICE_TOKEN` and persists its output through the same .NET-only write-back path (`POST /api/webhooks/trigger` HMAC or service-token REST). No other trigger source exists.

See `research/ai-integration.md` §2a for the authoritative orchestration contract.

**Engineering Excellence. Production Mindset. Professional Impact. Rocket**

## Implemented authentication contract (Feature 07)

Use the [auth contract](../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

Before committing or pushing implementation, run `python3 scripts/check-contract-sync.py` from
the repository root. Synchronize the owning spec, dependent specs, planning,
research, docs, contexts, agent instructions, diagram sources and progress notes
in the feature branch. Planned behavior must be labeled and must not count as
implemented acceptance evidence. Run the system verification gates as well.

## Multi-Tenant Migration Wave (2026-09-11 — PLANNED)

User-directed planning wave (canonical contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`; ERD amendment: `diagrams/erd/multi-tenant-amendment.md`). Griot becomes a multi-tenant platform: the operator (**SuperAdmin**) onboards Companies (`Organizations`); each company has an **Admin** (owner — ALL projects inside that company only), ProjectManagers manage their assigned projects, and **Clients** get a satisfaction-first portal. JWT v2 (backend 30): access token claims `name`/`org`/`role`/`perms`; refresh tokens stay opaque by design (NOT JWTs). AI adds **three new PLANNED specs** and a **client capability tier**:

- **`feature-specs/13-client-support-agent-and-client-boundary.md`** — client-facing agent answers only from client-scoped data (progress, public PM responses, handoff docs); feedback triage digests route to the assigned PM via backend notifications; golden transcripts assert the boundary (no cross-tenant data, no internal data to clients).
- **`feature-specs/14-handoff-user-manual-generator.md`** — generates the client user manual from project history + boards/columns + the handoff checklist; writes `HandoffDocuments` (`Kind = Manual`, `GeneratedByAi = true`) via backend 35 **through the spec 20 outbox**; idempotent and human-reviewable before submit; never invents credentials.
- **`feature-specs/15-maintenance-phase-triage-agent.md`** — post-deployment support triage: typed severity classification, KB/manual section suggestions (org-scoped), draft PM responses for human send; memory org-stamped.

**Client capability tier (PLANNED, backend 25 extension):** an effective `Client` role selects a reduced agent/manifest surface — client-scoped read tools + `submit_feedback` only. Internal capability tools (task/comment write proposals, executor plans, BI internals, raw logs, assignment/memory suggestions) are absent from client manifests; "what can you do?" answers from the live manifest; direct invocation of absent tools is denied server-side. Specs 01–12 each carry a "Multi-Tenant Update (2026-09-11 — PLANNED)" section: org-stamped payloads/events, per-org scheduling + tenant-aware budgets, org+role-context proposals, per-org reports (+ client progress variant), per-org executor bounds, org-stamped memory, SuperAdmin-delegated platform ops agent, org-scoped institutional memory and role/permission-aware (custom-role-respecting) suggestions. Nothing in this wave changes the implemented status of existing specs, and no production code exists yet.

## Audit synchronization — 2026-09-11

Implemented through backend 20 (observability pipeline); backend 29 (multi-tenant foundation) implemented 2026-09-11 on `feature/backend/29-multi-tenant-foundation-organizations` — roadmap §P0.5 next is backend 30 after 29 completes its outstanding gates. Future planning is not completed implementation. P0 (2026-09-11): backend 29 ✅ → 30 → 31 → 32 → 33 → 34 → 35 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10. P2: ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12. Full requirement/review ledger: `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`.
AI kit now has 12 planned specs. Institutional memory is ai 11; assignment suggestions ai 12. No direct SQL, auto-commit, silent assignment or borrowed mutation scopes.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
