# Cross-System Implementation Roadmap — Canonical Build Order

**Created:** 2026-09-10 · **Status:** Authoritative
**Companion docs:** `docs/DEPENDENCY-AUDIT.md` (within-system spec ordering) · `project-kit/context/integration-contracts.md` (wire contracts) · `scripts/check-contract-sync.py` (sync gate)

This file answers one question: **after finishing the current spec, which layer's which spec is next, and why?** Every progress tracker and system `AGENTS.md` points here. One spec at a time, one feature branch = one PR (hard rule) — so this is a single-threaded sequence; the "parallelizable" notes exist only so you know what *could* be pulled forward if the bootcamp calendar demands it.

## The Golden Rule of the Order

**A layer is "done enough" when everything downstream of it is unblocked — not when every spec is done.** Backend keeps three specs (09, 11, 10) after 08; they are not housekeeping: each one unlocks other systems.

## Phase Map (canonical sequence)

```
P0  Backend close-out   backend 09 → 11 → 10                     [Week 2 close-out]
P1  Web core            web 01 → 02 → … → 09                     [Week 3]
P2  AI hop + Copilot    ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05   [own-stack]
P3  Mobile              mobile 01 → 02 → … → 07                  [Week 4]
P4  Infra / DevOps      infra 01 → 02 → … → 07                   [Week 5]
P5  MCP                 mcp 01 → 02 → 03 → 04 → 05               [own-stack]
P6  Quality Engineering qa 01 → 02 → … → 13                     [Weeks 6–7]
```

### P0 — Backend 09 → 11 → 10 (finish the gateway specs)

| Next | Why it must be first |
|---|---|
| **09 AI service token + webhooks** | The single most-blocking remaining spec. `GRIOT_SERVICE_TOKEN` (restricted `ai-agent` principal) + HMAC `/api/webhooks/trigger` is the *only* door AI agents (ai 01–05) and MCP tools (mcp 03) may walk through. Nothing in `ai/` or `mcp/` can meet acceptance criteria without it. Depends only on spec 07 (done). |
| **11 Blob storage** | Needs only 01/02/04 (all ✅). Web 07 (task attachments) and mobile parity want real file upload/download URLs; shipping it in P0 means the Web phase never has to stop for a backend detour. |
| **10 API documentation** | Needs 04–08 (all ✅). Last backend spec; freezes the API surface into reference docs right before Web consumes it — and is the contract artifact QA 04/05 polish against. |

### P1 — Web 01–09 (Week 3 system, no AI needed)

Order: 01 (Vite setup) → 02 (MUI) → 03 (REST) → 04 (GraphQL) → 05 (secure auth) → 06 (public shell) → 07 (app shell) → 08 (kanban) → 09 (notifications).

Why Web next (not AI or MCP): 9 of 10 web specs depend **only** on backend specs that are all ✅ (04–08, 13–17, 07 auth). Web is the bootcamp's next graded week, the primary demo surface, and the theme work is already ahead of schedule (`web/src/theme.ts` exists from the design-system pass). Web 10 is deliberately *excluded* — see P2.

### P2 — AI hop: ai 01 → ai 02 → **web 10** → ai 03 → ai 04 → ai 05

This phase resolves the one circular dependency in the repo:

- **web 10 (Copilot panel) needs ai 02** (agent + Trigger realtime stream exposed) → implement ai 01–02 *first*.
- **ai 02 lists "web feature 10" as a dependency** — read that as a *contract-design* dependency (the panel is the stream's consumer), not a build-order one. `ai/project-kit/feature-specs/02-copilot-agent-streaming.md` carries this clarification.
- **ai 04 (propose-before-write) genuinely needs web 10** (approval cards are the UX the workflow wraps) → web 10 lands before ai 04.
- ai 05 (golden transcripts + budgets) closes the phase once 01–04 exist; it is the no-LLM-in-CI quality gate that AI/MCP/qa rely on.

ai 01's scaffold needs only Node 20 + backend 09 (P0) — so it cannot start earlier under the one-spec-at-a-time rule, and it does not need to.

### P3 — Mobile 01–07 (Week 4 system)

Order: 01 (Flutter setup) → 02 (auth screens) → 03 (REST/dio) → 04 (GraphQL) → 05 (Riverpod) → 06 (responsive UI) → 07 (notifications + device verification).

All mobile deps are backend specs (✅) plus mobile itself. Mobile goes *before* infra because Week 4 precedes Week 5 and because infra 05's CI should already have a Flutter test suite to wire. **Known cross-phase acceptance item:** mobile 07's "CI APK artifact" criterion is delivered by infra 05 (P4); the emulator + physical-device verification happens here in P3.

### P4 — Infra 01–07 (Week 5 system)

Order: 01 (Vercel web deploy) → 02 (backend Docker) → 03 (compose api+sqlserver) → 04 (full local topology) → 05 (GitHub Actions CI/CD) → 06 (Docker Hub + Railway deploy) → 07 (Netdata monitoring).

Infra runs after web + mobile so CI (infra 05) can build/test/deploy *every* surface in one pass, and so Vercel deploys a feature-complete web app. Infra 07 (Netdata) is a Phase-1 optimization gate — it must ship before public launch, so it cannot be deferred into P6.

### P5 — MCP 01–05 [own-stack]

Order: 01 (server setup) → 02 (tool roster) → 03 (service-token GraphQL client) → 04 (Docker + Railway deploy) → 05 (contract tests + Inspector).

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

## Phase-1 Optimization Gates (must ship before public launch)

Per root `AGENTS.md` rule 0a: blob storage (**backend 11 — P0**), Netdata monitoring (**infra 07 — P4**), dashboard caching + GraphQL DataLoader + pagination caps (embedded in backend specs 03/05/06 — re-verify during qa 10 p95 checks). Phase 2/3 optimizations stay evidence-gated behind k6 results (`docs/planning/OPTIMIZATION-RECOMMENDATIONS.md`).

## Change Protocol

This roadmap is a planning artifact: when a phase completes, update the root + owning system's `progress-tracker.md` **on the feature branch**, tick the phase here in the same PR, and run `python3 scripts/check-contract-sync.py`. If a dependency edge changes (new spec, reordered dependency), update this file + `docs/DEPENDENCY-AUDIT.md` + the affected specs in the same branch.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
