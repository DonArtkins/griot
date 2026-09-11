# AI Feature Spec 10 — SuperAdmin Ops Agent (Incident Summaries + Confirmed Broadcasts) [own-stack]

**Status:** PLANNED — the two trust models from research: (A) autonomous monitoring → SuperAdmin alerting (read-only, informational, NO confirmation) and (B) broadcast composition (send/mutate: ALWAYS confirm). Backend: spec 27. UI: web 12.

## A. Incident summarizer (autonomous, no confirmation)

- Triggered by backend 27's alert event (threshold breach).
- Fetches the incident window via the READ surface only (raw `ErrorLogs` rows are SuperAdmin/Dev-only per backend 25 — the job runs as a SuperAdmin-OBO identity when SuperAdmin-only data is needed; otherwise it works on the redacted summary).
- Drafts a plain-language incident summary: what broke, when, estimated affected users, likely cause (stack trace/AuditLogs diff) — reusing `LOGGING-AUDIT-REPORT.md` §5 query recipes.
- Returns the draft to backend 27, which creates Brevo `admin` + in-app delivery intents in one transaction; actual channel delivery is independent. No confirmation needed (small trusted audience, informational, one-directional).

## B. Broadcast composer (confirm gate, non-negotiable)

- SuperAdmin intent ("tell all users the site is down … remind 1h before") → drafts email + in-app copy → resolves the recipient scope to an ACTUAL count and list preview.
- **Nothing sends until the SuperAdmin confirms the preview in web 12** (one click; no retyping).
- On confirm: backend 27 creates the immediate send job, the T-1 reminder Trigger.dev delayed task, the in-app notification, and the AuditLogs row. Every step cancellable before fire (backend 27 cancel endpoint; web 12 cancels from the same card).
- Recipient resolution is a permission check at the tool layer (role-filter param validated against the caller's SuperAdmin authority — same RBAC principle as log access, applied to outbound actions).

## Boundaries

- The ops agent itself never holds delivery secrets: it produces copy + intents; backend 27 owns Brevo/Trigger.dev sends. No LLM key or Brevo key in this agent's context beyond its own.
- Idempotency: backend 27 keys bind actor, draft revision and audience hash. Re-click cannot double-send; a changed recipient list/time/copy requires a new preview and confirmation.
- After backend 20/27, everything lands in AuditLogs with the directing SuperAdmin's identity (backend 27) — no AI-initiated mass email without an audit trail.

## Dependencies

- ai 05 (golden transcripts + budgets) · ai 09 (manifest/thread scaffolding) · backend 20 (logs) · 24 (reports) · 25 (tiers) · 26 (threads) · 27 (alert/broadcast routes) · web 12 (confirm/cancel UI).

## Acceptance Criteria (all PENDING)

- [ ] Threshold event → summary drafted from the permitted data tier, sent without confirmation (golden transcript; no LLM in CI)
- [ ] Broadcast intent → draft + count preview; NOTHING sent until confirm (roster-level contract test: no send tool fires pre-confirm)
- [ ] Confirm → approval + immediate/T-1 delivery intents + AuditLogs persist atomically; external sends retry independently (backend 27 test)
- [ ] Cancel → nothing further fires
- [ ] Non-SuperAdmin identity cannot resolve `all_users` recipients (403 at the tool layer)

## Workspace role-check notices

An Owner/Admin may request a role-check reminder for their own workspace. Draft copy and show the backend-resolved recipient count, channel choices and role-diff evidence (or unknown). Human confirmation is required even for in-app-only delivery. No role assignment or change occurs. Non-SuperAdmin cannot target all platform users/admins. The agent has no notice-confirm/send tool.

## Type and Files Owned

New feature in `ai/src/ops/`: incident narrative and notice drafting with golden fixtures.

## Setup / Initialization

Use backend 27's deterministic audience/threshold policy, ai 01's pinned dependencies, ai security, Context7 and contract-sync. Summary callbacks are bound to the stored job; the agent cannot substitute an audience. No LLM availability dependency for urgent alerts: backend deterministic fallback remains available.

## Separation of Concerns

AI produces copy and cited hypotheses. Backend owns actual counts, authority, approval and delivery. Web 12 captures human confirmation. The same rules apply to workspace role-check notices and platform broadcasts, with different authorized audiences.

## Docker & Deploy

Existing Trigger cloud, backend and Brevo only. No new alert product, payment monitoring, scheduler container or direct delivery secret in AI/MCP.

## Verification

`npm run lint && npm run typecheck && npm test` (mocked LLM) — golden transcripts for summarizer + composer; MCP contract tests; Newman on backend 27 routes (post-27).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.