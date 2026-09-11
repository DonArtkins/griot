# AI Feature 13 — Client Support Agent & Client Boundary [own-stack]

**Status:** PLANNED — the client-facing axis of the multi-tenant wave. Backend owners: 34 (client portal data) / 25 (capability gateway extension) / 22 (feedback triage notifications). UI owners: web 15 / mobile 09.

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented**

## What This Delivers

A client-facing support agent that answers a `Client`-role user's questions **only** from client-scoped data for their `ProjectClients`-attached projects: progress (percent-complete, milestones, activity digest), public PM responses to feedback, and handoff documents. Client feedback/suggestions are summarized into a triage digest routed to the assigned ProjectManager via backend notifications. Golden transcripts assert the boundary — internal board internals, raw logs, other tenants' data and internal capability names never reach a client session.

## Dependencies

- ai 02 (copilot scaffolding), ai 05 (golden transcripts + budgets), ai 09 (manifest/thread scaffolding).
- Backend 09 ✅ (OBO), 29 (tenancy foundation), 25 (capability gateway extension — client capability tier), 26 (threads, org-stamped), 34 (client portal + `ClientFeedback`), 22 (org-scoped notifications).
- Consumers (not prerequisites): web 15, mobile 09.

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` (§6 client support surface)
- `ai/project-kit/context/architecture.md` + `ai/.agents/skills/ai-agent-security/SKILL.md`
- backend 25 (capability tiers) + backend 34 spec (client data contract)

## Agent Skills To Use

- `ai/.agents/skills/trigger-dev-tasks/SKILL.md`
- `ai/.agents/skills/ai-agent-security/SKILL.md`
- Context7 (framework docs) + contract-sync before any implementation branch.

## Files Owned

- `ai/src/client-agent/` (client copilot agent, client-scoped tools, feedback triage summarizer), typed schemas and golden fixtures.
- No backend/web/MCP code in this implementation branch.

## Implementation Notes

- Tools are client-scoped by construction: `get_project_progress`, `list_milestones`, `get_activity_digest`, `get_pm_responses`, `get_handoff_documents`, `submit_feedback` — each resolves project access from `ProjectClients` server-side before any data leaves.
- Effective role `Client` (JWT v2 `role` claim) selects this agent surface; every other agent/tool surface is absent from the manifest (capability gateway, backend 25 bump) — "what can you do?" answers from the live manifest.
- Feedback triage: the summarizer produces a bounded digest (new/acknowledged/resolved counts, open suggestions, flagged edit requests) routed to the assigned PM through backend 22 notifications — the agent never writes to boards or tasks on the client's behalf.
- Answers cite their sources (feedback IDs, milestone rows, handoff doc titles); missing data is disclosed as unknown, never filled from internal data.
- Org-stamped: all memory/threads carry `OrganizationId`; a client of org A never sees org B data. Suspended org → `403 org_suspended` before model spend.

## Separation of Concerns

- AI composes answers and triage digests from authorized client-scoped facts; backend owns client data authorization, `ClientFeedback` persistence and delivery; web 15 / mobile 09 own the human surfaces. The agent never performs board/task mutations.

## Docker & Deploy

- Existing Trigger cloud project, backend and notifications only. No new data store, vector database or direct SQL access. Budget keys partition per org.

## Out of Scope

- PM-side triage dashboards (web 15). Raw-log access (backend 25, SuperAdmin/Dev only). Board internals of any kind in client answers. Push delivery (backend notifications only).

## Acceptance Criteria

- [ ] A client session answers only from client-scoped data with citations; internal board internals/raw logs/other tenants' data never appear (golden transcript, mocked LLM)
- [ ] Feedback triage digest routes to the assigned PM via backend notifications; the client sees only status + public PM responses
- [ ] "What can you do?" is answered from the live client-tier manifest; internal capabilities are absent and direct invocation is denied server-side
- [ ] Cross-tenant question (org B project) is refused; suspended-org sessions fail closed before model spend
- [ ] Handoff-document questions answer only from `HandoffDocuments` visible to that client

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.