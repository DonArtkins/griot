# AI Feature Spec 09 — Agentic BI Copilot, Memory & Capabilities Manifest [own-stack]

**Status:** PLANNED — the "proper agentic BI copilot, not a chatbot bolted onto a corner widget" research wave (compare ThoughtSpot Sage / Tableau Pulse / Microsoft Copilot in Power BI). Frontend surface: web 12. Persistence: backend 26. Capability gating: backend 25. Reports: ai 07 via backend 24.

## What This Delivers

1. **Capabilities manifest — "what can you do".** The agent answers capability questions FROM `GET /api/me/capabilities` (backend 25), never a hardcoded static description. The manifest changes per role, so answers stay in sync as specs land.
2. **Conversation memory.** Threads resume (backend 26): a user returns to a sidebar thread and the Copilot picks it up with context callbacks.
3. **Context retrieval.** New turns call `GET /api/ai/conversations/{id}/context?q=` to retrieve prior messages + authorized context before answering. Backend 26 distinguishes chronological/lexical context from ai 11 curated semantic lessons.
4. **Lightweight personalization.** Reads `UserPreference` for sticky defaults (default report type, window, workspace) — explicitly NOT deep behavioral modeling (research caveat: overfitting to noise).
5. **Charts + report composition.** Answers can emit chart payloads / "add to report" / export actions (ai 07 pipeline) that web 12 renders inline (chart rendering is deterministic — the LLM produces a typed chart spec, never pixels).

## Boundaries (unchanged, applied per OBO role via backend 25)

- Read-only data plane + existing 4 scopes; report rows via `CreateReport` (spec 24) only with a matching scope.
- Raw logs / forensic data NEVER reach a Normal-User or Admin AI session — the tool is absent from that session's manifest (privilege-escalation fix, backend 25).
- No OTP/auth/MFA/delete/invite/member/broadcast tools (specs 23/27 — human-only).
- Every call: `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of: {real User.Id}`; retrieval attribution is implemented by backend 20 (currently planned).

## Dependencies

- ai 02 (copilot) · ai 05 (golden transcripts + budgets) · ai 06 (knowledge) · ai 07 (reports) · backend 16/20/24/25/26. Web 12 is a later consumer, not a build dependency.

## Acceptance Criteria (all PENDING)

- [ ] "What can you do?" answered from the live manifest; differs between a Normal-User and Admin session (golden transcript)
- [ ] Two-turn reference ("the report we discussed") resolves via the context endpoint (backend 26)
- [ ] Chart payload renders deterministically in web 12 from a typed spec (fixture-identical)
- [ ] Raw-log tool absent from non-SuperAdmin/Dev manifests; a forbidden tool request → backend 403 before any data leaves
- [ ] Preferences applied as defaults; overridden by explicit ask

## Type and Files Owned

New feature in `ai/src/copilot/`: manifest use, context assembly and typed answer/chart schemas.

## Setup / Initialization

Use ai 01's pinned dependencies and backend 26's private threads. Backend stores replies via a durable callback bound to the originating job/thread/user; no conversation write is authorized by CreateNotification. Use contract-sync, ai security and Context7 skills.

## Separation of Concerns

Backend owns permissions/history/preferences; AI composes allowed answers; web 12 renders typed chart data with source rows, units, time window and missing-data notices. No model-produced script, executable HTML or chart code. No public embedding in v1.

## Docker & Deploy

Reuse Trigger cloud and backend storage; no new service or vector database. Cross-project lesson retrieval arrives in ai 11; v1 turn retrieval is not a claim of organizational memory.

## Verification

`npm run lint && npm run typecheck && npm test` (mocked LLM, no network in CI) — golden transcripts incl. manifest-driven + memory-turn fixtures.

## Multi-Tenant Update (2026-09-11 — PLANNED)

- Conversation memory is org-stamped: threads and context retrieval are keyed by (user, organization); switching organizations starts a fresh thread surface — org A memory is never retrieved in org B sessions (backend 26/29).
- Capabilities-manifest answers ("what can you do?") reflect the active org's effective role and `perms` claim, including `custom:{roleId}` roles — answers change per role AND per org.
- Chart payloads and "add to report" actions are computed within the active org's data only; report composition rides ai 07's per-org pipeline.
- Client-role sessions use only the client boundary surface (ai 13) — the BI copilot never renders internal BI data into a client thread.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.