# AI Feature Spec 10 — SuperAdmin Ops Agent (Incident Summaries + Confirmed Broadcasts) [own-stack]

**Status:** PLANNED — the two trust models from research: (A) autonomous monitoring → SuperAdmin alerting (read-only, informational, NO confirmation) and (B) broadcast composition (send/mutate: ALWAYS confirm). Backend: spec 27. UI: web 12. One feature branch: `feature/ai/10-superadmin-ops-agent`.

## A. Incident summarizer (autonomous, no confirmation)

- Triggered by backend 27's alert event (threshold breach).
- The agent receives ONLY the **permitted incident snapshot** that backend 27 builds from the operator-provisioned policy — never raw `ErrorLogs`, stack traces, or SuperAdmin-OBO reads. Backend 25 raw-log reads stay SuperAdmin/Dev-only and are never delegated to AI. Backend 27 records the outbox event + job identity for the summary run, and rejects any model-selected `X-On-Behalf-Of` value.
- Before anything reaches the AI provider, backend 27 redacts the snapshot: sensitive content (secrets, tokens, PII, personal data, unrelated users) is removed from error/stack-trace/audit-diff material. A fixed output allowlist (what broke, when, estimated affected users, likely cause category, no raw snippets of secrets or other users' data) is applied to the generated draft before backend 27 creates Brevo `admin` + in-app delivery intents in one transaction; actual channel delivery is independent. No confirmation needed (small trusted audience, informational, one-directional), but the draft never contains data outside the allowlist.

## B. Broadcast composer (confirm gate, non-negotiable)

- SuperAdmin intent ("tell all users the site is down … remind 1h before") → drafts email + in-app copy → resolves the recipient scope to an ACTUAL count and list preview.
- **Nothing sends until the SuperAdmin confirms the preview in web 12** (one click; no retyping).
- On confirm: backend 27 creates the immediate send job, the T-1 reminder Trigger.dev delayed task, the in-app notification, and the AuditLogs row. Every step cancellable before fire (backend 27 cancel endpoint; web 12 cancels from the same card).
- Recipient resolution: the tool's role-filter check is an **optional early validation only** — backend 27 authoritatively resolves recipients and rechecks authorization during BOTH preview and confirmation, using the caller's backend-verified SuperAdmin authority (same RBAC principle as log access, applied to outbound actions). Confirmation is bound to the actor, the exact draft revision and an audience hash; direct, replayed or modified requests that bypass the tool layer are rejected.

## Boundaries

- The ops agent itself never holds delivery secrets: it produces copy + intents; backend 27 owns Brevo/Trigger.dev sends. No LLM key or Brevo key in this agent's context beyond its own.
- Delivery idempotency: backend 27 keys are scoped **per recipient, channel and event** (not just per batch) and provider delivery status is persisted for each key. Workers check per-delivery keys and persisted statuses before every send, so retries skip accepted/delivered items and persist partial responses individually. Ambiguous delivery outcomes require reconciliation or operator resolution before retry, as specified by backend 27. Re-click cannot double-send; a changed recipient list/time/copy requires a new preview and confirmation.
- After backend 20/27, everything lands in AuditLogs with the directing SuperAdmin's identity (backend 27) — no AI-initiated mass email without an audit trail.

## Dependencies

- ai 05 (golden transcripts + budgets) · ai 09 (manifest/thread scaffolding) · backend 20 (logs) · 24 (reports) · 25 (tiers) · 26 (threads) · 27 (alert/broadcast routes) · web 12 (confirm/cancel UI).

## Acceptance Criteria (all PENDING)

- [ ] Threshold event → summary drafted from the permitted data tier, sent without confirmation (golden transcript; no LLM in CI)
- [ ] Broadcast intent → draft + count preview; NOTHING sent until confirm (roster-level contract test: no send tool fires pre-confirm)
- [ ] Confirm → approval + immediate/T-1 delivery intents + AuditLogs persist atomically; external sends retry independently (backend 27 test)
- [ ] Cancel → nothing further fires
- [ ] Non-SuperAdmin identity cannot resolve `all_users` recipients (403 at backend preview and confirmation even when the optional tool check is bypassed)

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

## Planning PR review gate

The user approved keeping this planning correction on the backend 09 review branch for this batch on 2026-09-11, directing "COMMIT AND PUSH TO GITHUB" in response to the exception request. See `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md` for the bounded exception. Future production implementation still requires its own feature branch/PR; this approval does not approve schema implementation or merge.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
