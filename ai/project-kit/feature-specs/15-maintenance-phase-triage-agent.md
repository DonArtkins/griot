# AI Feature 15 — Maintenance-Phase Triage Agent [own-stack]

**Status:** PLANNED — post-deployment support triage for `Maintenance`/`PostDeploymentSupport` projects. Backend owners: 35 (maintenance requests) / 22 (org-scoped notifications) / 26 (org-stamped memory). UI owners: web 16 / mobile 09.

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented**

## What This Delivers

Post-deployment support triage: when a client raises a maintenance/support request, the agent classifies severity, suggests knowledge-base/manual sections (including ai 14's generated manual) that may answer it, and drafts PM responses for human review. The client keeps the satisfaction-first portal (read-only + maintenance requests); the PM keeps every write decision. Memory is org-stamped — triage lessons never cross tenants.

## Dependencies

- ai 02/05/09 (copilot, transcripts, threads), ai 14 (generated manual sections as KB source), ai 11 (org-scoped institutional memory).
- Backend 09 ✅ (OBO), 20 (outbox attribution), 22 (org-scoped notifications), 25 (capability gateway — client tier), 26 (memory, org-stamped), 29 (tenancy), 35 (maintenance requests + post-deployment support).
- Consumers (not prerequisites): web 16, mobile 09.

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` (§7 maintenance phase; §6 client boundary via ai 13)
- backend 35 spec (maintenance request contract), ai 13 (client boundary), ai 14 (manual sections)
- `ai/project-kit/context/architecture.md` + `ai/.agents/skills/ai-agent-security/SKILL.md`

## Agent Skills To Use

- `ai/.agents/skills/trigger-dev-tasks/SKILL.md`
- `ai/.agents/skills/ai-agent-security/SKILL.md`
- Context7 + contract-sync before any implementation branch.

## Files Owned

- `ai/src/maintenance/` (severity classifier, KB/manual section matcher, response drafter), typed schemas and golden fixtures.
- No backend/web/MCP code in this implementation branch.

## Implementation Notes

- Severity classification is a typed proposal (`Low`/`Medium`/`High`/`Critical`) with cited evidence (symptoms, affected flows, manual sections, prior similar requests) — never an opaque score; the PM confirms or overrides, and overrides become labeled training/lesson data.
- KB/manual section suggestions: match the request against ai 14's generated manual sections and ai 11 org-scoped lessons; retrieval filters by `OrganizationId` first — org B's manual sections never answer org A's request.
- Draft PM responses: bounded, cited drafts routed to the assigned PM via backend 22 notifications; the PM edits/sends as the authenticated human (the agent never sends directly to clients).
- Client-visible surface stays inside the ai 13 client boundary: the client sees their request, its status, and the PM's published response — never severity internals, internal candidates or other clients' requests.
- All memory/threads org-stamped; suspended-org requests queue with `403 org_suspended` semantics on org-scoped fan-out; idempotency keys org-stamped so retries cannot leak digests across tenants.

## Separation of Concerns

- AI classifies, suggests and drafts; the backend 35 owns maintenance-request state and persistence; backend 22 owns delivery; the PM (web 16 / mobile 09) owns every client-facing send. AI never resolves requests or writes PM responses as the PM.

## Docker & Deploy

- Existing Trigger cloud project and AI provider only; org-partitioned budgets. No new container, vector store or delivery secret in AI.

## Out of Scope

- Automated severity-based escalation to SuperAdmin (backend 27 incident alerting owns platform incidents). Autonomous client replies. Code-deployment tools (never exist for AI). Handoff generation (ai 14).

## Acceptance Criteria

- [ ] A seeded maintenance request yields a typed severity classification with cited evidence; PM override is recorded and honored
- [ ] KB/manual suggestions cite org-scoped sections only; org B's manual never answers org A's request (golden transcript)
- [ ] Draft responses route to the assigned PM via backend notifications; the agent never sends to clients directly
- [ ] Client sees request + status + published response only — no severity internals or other clients' requests
- [ ] Triage memory is org-stamped; retries with org-stamped idempotency keys cannot duplicate digests across tenants
- [ ] Suspended-org requests fail closed on org-scoped fan-out before model spend

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.