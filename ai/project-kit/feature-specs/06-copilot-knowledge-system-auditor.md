# AI Feature 06 — Knowledge Agent and Scoped System Auditor [own-stack]

**Status:** PLANNED. Read-only analysis plus explicitly requested report generation; no auth/OTP inspection.

## Type

New AI capability over existing authorized backend data.

## What This Delivers

Answer project/workspace questions with source citations. Summarize stale work, incomplete evidence, operational symptoms and permitted audit activity. Missing evidence is unknown, not a successful audit.

## Capability map

| Question | Permitted source |
|---|---|
| Work progress, blockers, workload | scoped tasks, boards, dashboard and activity |
| Workspace audit/health | backend 24/25 sanitized audit-summary and health projection for Owner/Admin |
| Technical incident investigation | backend 25 raw-log tool only for SuperAdmin/Dev with matching workspace delegation |
| Deployment/test readiness | backend 28 structured evidence |

No 2FA coverage, OTP state, password data, raw Admin log access or invite-management tools. The capabilities manifest is the source of available tools. Do not count excluded/inaccessible rows for a lower-tier user.

## Dependencies

Ai 01/02/05; backend 09/18/20/24/25/28. Web 10 is available for interaction; web 11 is a later consumer, not a dependency.

## Context To Read First

`research/ai-integration.md`, backend 25, ai security skill, Context7 and contract-sync.

## Files Owned

`ai/src/agents/system-auditor.ts`, knowledge tools, typed schemas and golden fixtures.

## Setup / Initialization

Use the pinned AI toolchain and shared API client from ai 01. Fetch the live manifest for each user/workspace. Obtain identity from the backend job or approved schedule; never model text. Fixtures cover Member, Owner/Admin and platform SuperAdmin/Dev separately.

## Separation of Concerns

AI interprets authorized facts; backend enforces scope/data tier and persists requested reports through backend 24. Audits do not approve deployments or certify tests. Notification delivery stays in backend 22/27.

## Docker & Deploy

Existing Trigger cloud project and cost budget. No additional data store, webhook trigger source or direct SQL access.

## Acceptance Criteria

- [ ] Answers cite the rows/evidence and time window used; missing/retained-away data is disclosed.
- [ ] Seeded stale tasks, incomplete deployment evidence and privileged incident spikes produce correct, scoped findings.
- [ ] Member/Admin sessions never receive raw logs, OTP state or cross-workspace snippets.
- [ ] Requested audit report uses backend 24 CreateReport; read-only answers do not create unsolicited reports or notices.
- [ ] Forbidden tools are absent and direct invocation is denied server-side.

## Verification

`npm run lint && npm run typecheck && npm test`; mocked LLM, manifest and authorization golden tests.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
