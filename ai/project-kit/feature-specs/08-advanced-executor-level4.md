# AI Feature 08 — Approved Multi-Step Executor [own-stack]

**Status:** PLANNED. Execute only explicitly scoped operations; never claim everything the user can do is available to AI.

## Type

New planning-loop orchestration over the existing allowed tool roster.

## What This Delivers

“Find overdue work, draft comments for these two tasks, then produce a summary” becomes a visible ordered plan. Human approval binds exact steps, targets, content and expected revisions. READ → PROPOSE → APPROVE → EXECUTE → OBSERVE → summarize. Task status/assignment changes, role management, auth/OTP, invitations and deletes remain human operations outside this executor.

## Dependencies

Ai 02/04/05/06/07, backend 09/20/24/25, web 10/11. Build after web 11. Backend 20's idempotency contract is a hard prerequisite, not an assumed Trigger.dev guarantee.

## Context To Read First

`research/ai-integration.md`, backend 09/20/24/25, ai security skill, Context7 and contract-sync.

## Files Owned

`ai/src/executor/` planner, approval-bound executor, result validation and golden transcripts.

## Setup / Initialization

Use the shared authenticated client and backend capability manifest. Current grant vocabulary: ReadWorkspace, CreateTask, AddComment, CreateNotification; CreateReport only after backend 24 and only when explicitly delegated. Scope names do not prove a route exists: today no generic OBO notification-create route exists; backend 22 must define it before the executor exposes that tool.

## Execution controls

- Every model-driven mutation/notification appears in the approved plan. Content/recipient/target/version changes require a new approval; approval is stored server-side with human actor and plan hash, never inferred from model output.
- Use backend `X-Idempotency-Key`, scoped to user/workspace/operation and step. Persist canonical request hash and response/job result for 24 hours atomically with the mutation. Same key/same request returns the saved result; different request returns 409. Pending concurrent requests return a retryable in-progress result without a second write. Keys are derived from persisted plan+step IDs, not just freeform prompt text.
- Resume partially completed plans from stored outcomes. Never blindly rerun notifications after a timeout. Cancel stops pending steps; completed effects are shown honestly and are not automatically undone.
- Validate typed tool output before planning the next step. Ambiguous goals ask for clarification. Task text and tool output are untrusted data and cannot expand the approved plan.
- Role/membership/grant expiry is checked at execution, not only planning. State-changing steps record runId, stepId, approver and outcome through backend 20.
- Report the execution result in the thread. Persist an ai_action_summary report for requested summaries/substantial multi-step changes, not for every read-only tool call.

## Separation of Concerns

AI plans and executes allowed approved steps. Web captures human approval and separately performs permitted human-only edits. Backend validates approval provenance, scopes and idempotency. Bulk notices use backend 27's own confirmation, never generic CreateNotification to bypass recipient review.

## Docker & Deploy

Existing Trigger cloud retry primitives and backend durable jobs. No new container or extra OBO scopes for status updates, member changes or code patches.

## Acceptance Criteria

- [ ] A visible plan requires approval before its first mutation; injection text does not change steps.
- [ ] Partial failure resumes without duplicate task/comment/notification/report writes.
- [ ] Same idempotency key/different payload returns 409; concurrent retries cannot double-fire.
- [ ] Revoked permissions, changed state, cancellation or edited plan prevent remaining writes until re-approved.
- [ ] No status-update/delete/invite/auth/role tool appears; unsupported steps are handed to the human flow.
- [ ] Every executed step has attributable audit metadata; result reports reflect actual successes/failures.

## Verification

`npm run lint && npm run typecheck && npm test`; golden transcripts with mocked LLM and backend failure/retry scenarios.

## Multi-Tenant Update (2026-09-11 — PLANNED)

- Executor bounds are per org: every approved plan is bound to the organization in effect at approval time (from the JWT v2 `org` claim), and execution re-checks active-org match at each state-changing step — a mismatch returns 403 before any write.
- A `Client` role never receives executor capabilities: internal capability tools (task/comment write proposals, multi-step plans) are absent from client manifests; clients get only the feedback-shaped surface (ai 13), enforced by the capability gateway (backend 25 bump).
- Custom roles (`custom:{roleId}`) bound plans to the permission keys in the token `perms` claim; role/membership/grant expiry re-checks happen at execution, per org.
- Idempotency keys and audit rows become org-stamped (`org:{orgId}:…`), keeping step attribution per tenant for backend 25 log tiers.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
