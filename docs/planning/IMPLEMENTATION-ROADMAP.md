# Cross-System Implementation Roadmap — Canonical Build Order

**Created:** 2026-09-10 · **Status:** Authoritative
**Companion docs:** `docs/DEPENDENCY-AUDIT.md` (within-system spec ordering) · `project-kit/context/integration-contracts.md` (wire contracts) · `scripts/check-contract-sync.py` (sync gate)

**2026-09-12 checkpoint (updated):** backend 29 is IMPLEMENTED on
`feature/backend/29-multi-tenant-foundation-organizations` (commit `9447e15`, pushed
after explicit user approval; review-fix follow-up `52cb862` pushed 2026-09-12), backend 30
is IMPLEMENTED on `feature/backend/30-auth-jwt-v2`, and backend 31 is IMPLEMENTED on
`feature/backend/31-rbac-roles-custom-permissions`. Its §P0.5 order governs: 29 ✅ → 30 ✅ → 31 ✅ →
32 → 33 → 34 → 35, then hardening — never older shorthand that goes directly from 20 to 18.
**Specs 29 (✅ COMPLETE, all gates closed 2026-09-11) and 30 are ✅ IMPLEMENTED
(2026-09-12: JWT v2 claims + org session + SuperAdmin bootstrap; build 0W/0E;
164 passed / 8 skipped), and spec 31 is ✅ IMPLEMENTED (2026-09-12: RBAC v2 —
seeded system roles + custom-role CRUD + force-revoke + server-side permission
enforcement via `PermissionService`; build 0W/0E; 187 passed / 8 skipped / 0 failed).
Backend 32 (Company onboarding & platform management, SuperAdmin) is next on its own
feature branch.**
See the [preflight report](BACKEND-29-PREFLIGHT-2026-09-11.md) for the pre-implementation
snapshot.

This file answers one question: **after finishing the current spec, which layer's which spec is next, and why?** Every progress tracker and system `AGENTS.md` points here. One spec at a time, one feature branch = one PR (hard rule) — so this is a single-threaded sequence; the "parallelizable" notes exist only so you know what *could* be pulled forward if the bootcamp calendar demands it.

## The Golden Rule of the Order

**A layer is "done enough" when everything downstream of it is unblocked — not when every spec is done.** The P0 sequence below includes the backend evidence, authorization and durability gates needed by later systems; numerical order alone is not the build order.

## Phase Map (canonical sequence)

```
P0  Backend close-out   backend 09 → 20 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10   [Week 2 close-out + AI-superpowers/2FA/BI/ops wave]
P0.5 Multi-tenant wave  backend 29 → 30 → 31 → 32 → 33 → 34 → 35 (+ revisions 36–51; web 13–16, mobile 08–09, ai 13–15, mcp 07, qa 14)   [2026-09-11 multi-tenant migration wave — see §P0.5 below]
P1  Web core            web 01 → 02 → … → 09                     [Week 3]
P2  AI hop + Copilot    ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12   [own-stack + superpowers + BI/ops wave]
P3  Mobile              mobile 01 → 02 → … → 07                  [Week 4]
P4  Infra / DevOps      infra 01 → 02 → … → 07                   [Week 5]
P5  MCP                 mcp 01 → 02 → 03 → 04 → 05 → 06               [own-stack; 06 = v2 report/audit tools]
P6  Quality Engineering qa 01 → 02 → … → 13                     [Weeks 6–7]
```

### P0 — Backend 09 → 20 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10 (gateway specs + hardening wave + critical-action-OTP/reports + BI/ops wave)

| Next | Why it must be first |
|---|---|
| **09 AI service token + webhooks** | ✅ **DELIVERED** on `feature/backend/09-ai-service-token-and-webhooks` (2026-09-11). `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of: {real User.Id}` → real-user OBO principal (role `ai-on-behalf-of`, 4 scopes, no deletes/invites) + HMAC `POST /api/webhooks/trigger` (`WebhookHmacMiddleware`). The A→.NET door AI agents (ai 01–05) and MCP tools (mcp 03) walk through is now open. Contract: `docs/api/ai-service-token-contract.md`. |
| **20 Observability & logging pipeline** | ✅ **DELIVERED** on `feature/backend/20-observability-logging-pipeline` (2026-09-11). All four tables now have writers: `ApiLoggingMiddleware` (every request incl. 401/404/429, masked IPs, redacted query secrets), global handler → `ErrorLogs` + RFC 7807 ProblemDetails, `IAuditService` audit rows committed in the same transaction as every state-changing write (REST + GraphQL + auth events), `ActivityLogs` feed writer, `usp_PruneObservabilityLogs` retention (SQL-gated integration test green: 91/366-day boundary rows pruned, unfixed errors survive, idempotent). RunId/index migration `AddObservabilityAuditRequestIdAndIndexes` applied under the approved ERD amendment. Durable outbox/webhook-inbox/idempotency store remains a PLANNED gate inside spec 20 until Trigger.dev callers ship. |
| **18 Search, filter, paginate, sort** | Uniform capped/whitelisted query contract over all list endpoints — the read plane that 19's cache keys and 21's scale-out assume. Builds directly on 20's log routes. |
| **19 Caching & rate limiting** | Redis cache-aside (dashboard/board/unread) + per-route limiter partitions (auth/webhook/GraphQL) + GraphQL cost caps. Protects everything 20–18 built; uses 18's query shapes for cache keys. |
| **22 Notification fan-out (in-app + email)** | Needs 20's event hooks (audit/activity writes) and 19's limiter partitions; reuses spec-12 email infra. Gives web 09 / mobile 07 a live producer instead of empty reads. |
| **21 DB triggers, backups, restore readiness** | Audit triggers write the same `AuditLogs` table 20 fills — triggers land **after** 20 so dedupe conventions exist. Backup chain + restore drill are the "attack/crash cannot render us useless" guarantee. Infra-side compose sync rides infra 03/06 (P4). |
| **23 Critical-action OTP & step-up [own-stack]** | User-directed security wave (2026-09-11): login 2FA enforcement (202-on-challenge), forgot/reset password, delete account, guarded-op step-up (`RequireStepUp`). **Human-only — AI OBO callers are 403, permanently.** Needs 07 (OTP), 12 (Brevo verified sender), 20 (audit rows), 16 (guarded routes). |
| **11 Blob storage** | Needs only 01/02/04 (all ✅). Web 07 (task attachments) and mobile parity want real file upload/download URLs; shipping it before P1 means the Web phase never stops for a backend detour. It also hosts spec-24 report artifacts. |
| **28 Project lifecycle evidence** | Small structured readiness/deployment/test-run evidence surface; needed before backend 24 can truthfully gate CAB/post-deployment/regression templates. No phase enum or deployment platform. |
| **24 AI reports & export surface [own-stack]** | Report rows + PDF/CSV artifacts (blob 11) + `audit-summary`; FIFTH OBO scope `CreateReport` (new scoped, auditable capability — never a loosened grant). Unblocks ai 07, web 11, mcp 06. Needs 11 (blob), 20 (audit), 28 (lifecycle evidence), 16 (reads), 09 (OBO). |
| **25 Role-tiered logs/capability gateway** | Backend resolves role/scope tools; raw logs privileged, Admin summaries redacted. |
| **26 Conversations and curated memory** | Private threads, explicit preferences and permission-filtered lessons; callbacks bound to durable jobs. |
| **27 Incident alerts and confirmed notices** | Fixed SuperAdmin alerts plus human-confirmed broadcasts/workspace role-check reminders. |
| **10 API documentation** | Needs 04–08 + the hardened surface (18–28) documented, including the 25–28 routes/capability contracts and spec-10 as the last backend spec; freezes the API surface into reference docs right before Web consumes it — and is the contract artifact QA 04/05 polish against. Any surface excluded is documented explicitly with rationale in the spec. |

### P0.5 — Multi-Tenant Migration Wave (2026-09-11, PLANNED — backend 29–35 new + 36–51 revisions; cross-system bumps)

**Rationale (user-directed, research-backed):** Griot becomes a multi-tenant platform — the operator (SuperAdmin) onboards **Companies (Organizations)**, each company has an **Admin (owner)** who sees ALL projects inside their company only, ProjectManagers manage their projects, Members execute, and **Clients** get a satisfaction-first portal (progress view + feedback + handoff + maintenance). Research: `research/LYNCXS-MULTI-TENANT-SYSTEMS-ENGINEERING.md`; canonical contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`; planned ERD amendment: `diagrams/erd/multi-tenant-amendment.md`.

**Spec-numbering rules of this wave (do not deviate):**
- Implemented backend specs (01–09, 12, 13–17, 20) are **frozen** — their multi-tenant behavior is delivered by NEW revision specs **36–51** (one per implemented spec, mapping 36→01 … 51→20). The originals stay untouched.
- Unimplemented backend specs (10, 11, 18, 19, 21–28) are **modified in place** (each carries a "Multi-Tenant Update (2026-09-11 — PLANNED)" section).
- NEW multi-tenant feature specs are **29–35** (foundation, JWT v2, RBAC, onboarding, offboarding, client portal, handoff/maintenance).
- Cross-system: all other layers' existing specs are modified in place, and new specs added — **web 13–16** (company admin console, SuperAdmin platform console, client portal UI, handoff/maintenance UI), **mobile 08–09** (org switching, client portal parity), **ai 13–15** (client support agent + boundary, handoff manual generator, maintenance triage), **mcp 07** (tenant-scoped tools v3), **qa 14** (multi-tenant isolation suite).

**Canonical implementation order after spec 20:** **29 → 30 → 31 → 32 → 33 → 34 → 35** (tenant foundation first — every later spec assumes `OrganizationId` and the JWT v2 claims exist) → then the existing P0 hardening order **18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27** → revision specs **36–51** land with the implementation branch that touches their base feature (priority: 37, 39, 40, 46–49, then the rest) → **10** (API docs) last, freezing the full multi-tenant surface. Every spec still ships one-at-a-time on its own feature branch (hard rule).

### P1 — Web 01–09 (Week 3 system, no AI needed)

Order: 01 (Vite setup) → 02 (MUI) → 03 (REST) → 04 (GraphQL) → 05 (secure auth) → 06 (public shell) → 07 (app shell) → 08 (kanban) → 09 (notifications).

Why Web next (not AI or MCP): 9 of 12 web specs depend **only** on backend specs that are all ✅ (04–08, 13–17, 07 auth). Web is the bootcamp's next graded week, the primary demo surface, and the theme work is already ahead of schedule (`web/src/theme.ts` exists from the design-system pass). Web 10 is deliberately *excluded* — see P2.

### P2 — AI hop: ai 01 → ai 02 → **web 10** → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → **web 11** → ai 08 → ai 09 → **web 12** → ai 10 → ai 11 → ai 12

All frontend references in earlier backend/AI specs are consumer contracts, not backwards build dependencies. In particular ai 06/07 do not wait for web 11, ai 09 does not wait for web 12, and web 12 builds generic cards before ai 10–12 integrate. The ai 02/web 10 handshake remains:

**2026-09-11 superpowers wave** — after ai 05 → ai **06** (Copilot knowledge agent + system auditor) → ai **07** (report generation PDF + CSV, needs backend 24) → **web 11** (Reports & Audit Center) → ai **08** (Level-4 executor). **2026-09-11 BI/ops wave** — ai **09** (agentic BI copilot + capabilities manifest + memory, needs backend 25/26) → **web 12** (dedicated AI Workspace sidebar: charts, threads, report composition, SuperAdmin ops console) → ai **10** (SuperAdmin incident summarizer + confirmed-broadcast composer, needs backend 27). No AI spec ever touches auth/OTP (backend 23 human-only); report rows ride the scoped `CreateReport` capability (backend 24); per-role tool filtering is server-side (backend 25); broadcasts always confirm before any send (backend 27).

- **web 10 (Copilot panel) needs ai 02** (agent + Trigger realtime stream exposed) → implement ai 01–02 *first*.
- **ai 02 lists "web feature 10" as a dependency** — read that as a *contract-design* dependency (the panel is the stream's consumer), not a build-order one. `ai/project-kit/feature-specs/02-copilot-agent-streaming.md` carries this clarification.
- **ai 04 (propose-before-write) genuinely needs web 10** (approval cards are the UX the workflow wraps) → web 10 lands before ai 04.
- ai 05 (golden transcripts + budgets) gates the first AI baseline once 01–04 exist; it is the no-LLM-in-CI quality gate that AI/MCP/qa rely on.

ai 01's scaffold needs only Node 20 + backend 09 (P0) — so it cannot start earlier under the one-spec-at-a-time rule, and it does not need to.

### P3 — Mobile 01–07 (Week 4 system)

Order: 01 (Flutter setup) → 02 (auth screens) → 03 (REST/dio) → 04 (GraphQL) → 05 (Riverpod) → 06 (responsive UI) → 07 (notifications + device verification).

All mobile deps are backend specs (✅) plus mobile itself. Mobile goes *before* infra because Week 4 precedes Week 5 and because infra 05's CI should already have a Flutter test suite to wire. **Known cross-phase acceptance item:** mobile 07's "CI APK artifact" criterion is delivered by infra 05 (P4); the emulator + physical-device verification happens here in P3.

### P4 — Infra 01–07 (Week 5 system)

Order: 01 (Vercel web deploy) → 02 (backend Docker) → 03 (compose api+sqlserver) → 04 (full local topology) → 05 (GitHub Actions CI/CD) → 06 (Docker Hub + Railway deploy) → 07 (Netdata monitoring).

Infra runs after web + mobile so CI (infra 05) can build/test/deploy *every* surface in one pass, and so Vercel deploys a feature-complete web app. Infra 07 (Netdata) is a Phase-1 optimization gate — it must ship before public launch, so it cannot be deferred into P6.

### P5 — MCP 01–06 [own-stack]

Order: 01 (server setup) → 02 (tool roster) → 03 (service-token GraphQL client) → 04 (Docker + Railway deploy) → 05 (contract tests + Inspector) → 06 (v2 report/audit tools — needs backend 20/24/25 role-aware filtering + ai 06/07).

MCP 03 needs backend 09 + 05 (✅ after P0). MCP 04 needs infra 02–04/06 (✅ after P4) — that is the *only* reason MCP sits behind infra, and it is why finishing infra first lets the whole MCP phase run against a deployed, stable API. mcp 01–02 are technically unblocked at any time (Node 20 only) and may be pulled forward into an idle gap.

### P6 — QA 01–13 (Weeks 6–7)

Order: 01 (QE fundamentals) → 02 (manual testing) → 03 (Jira test mgmt) → 04 (Postman mastery, extends backend 08's collection) → 05 (Newman in CI, needs infra 05) → 06 (xUnit) → 07 (Jest+RTL) → 08 (Flutter) → 09 (Cypress) → 10 (k6/OWASP/a11y) → 11 (>80% coverage gate) → 12 (manual cycles + defects) → 13 (UAT + exec report).

QA is last because it verifies the other six systems and needs the deployed topology (Railway/Vercel) for manual + UAT cycles. qa 01–03 are process docs with zero code dependencies and may run in any idle gap.

## What Unlocks What (cross-system edge list)

| Upstream | Unblocks |
|---|---|
| backend 04–08, 13–17 ✅ | web 01–09, mobile 01–07, qa 04/06 |
| backend 07 (auth) ✅ | web 03/05, mobile 02/03, web 10 |
| backend 09 | ai 01–05, mcp 03, Copilot stream write-back |
| backend 11 (blob) | attachment upload UI in web 07 + mobile parity |
| backend 10 (API docs) | qa 04/05 contract polish |
| web 01–02 | infra 01 (Vercel deploy) |
| web 05 + 07 | web 08, 09, 10 |
| ai 01–02 | web 10 (Copilot panel) |
| web 10 | ai 04 (propose-before-write approval cards) |
| web + mobile complete | infra 05 (CI over all surfaces), mobile 07 APK artifact |
| infra 02–04, 06 | mcp 04 (Railway deploy) |
| infra 05 | qa 05 (Newman in CI), qa 11 (coverage gate) |
| everything deployed | qa 10–13 (k6 / OWASP / UAT against the deployed system) |

## Final P2 additions

After ai 10, ai 11 adds curated institutional memory and reviewable reuse/fix suggestions; ai 12 adds explainable candidate suggestions with human task/role writes. Both reuse web 12 review cards. PR hooks/editor integration, automatic code application, public report sharing and a separate vector database remain deferred.

## Phase-1 Optimization Gates (must ship before public launch)

Per root `AGENTS.md` rule 0a: blob storage (**backend 11 — P0**), Netdata monitoring (**infra 07 — P4**), dashboard caching + GraphQL DataLoader + pagination caps (embedded in backend specs 03/05/06 — re-verify during qa 10 p95 checks). Phase 2/3 optimizations stay evidence-gated behind k6 results (`docs/planning/OPTIMIZATION-RECOMMENDATIONS.md`).

## Change Protocol

This roadmap is a planning artifact: when a phase completes, update the root + owning system's `progress-tracker.md` **on the feature branch**, tick the phase here in the same PR, and run `python3 scripts/check-contract-sync.py`. If a dependency edge changes (new spec, reordered dependency), update this file + `docs/DEPENDENCY-AUDIT.md` + the affected specs in the same branch.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
