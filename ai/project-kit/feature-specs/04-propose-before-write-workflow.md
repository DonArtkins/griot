# Feature 04 - Propose-Before-Write Workflow

## Type

NEW FEATURE ([own-stack])

## What This Delivers

The approval gate: the Copilot proposes mutations (`create_task`, `update_task_status`, `add_comment`) as structured proposals; the web panel renders approval cards; the APP executes the mutation through the normal REST path.

## Dependencies

- Feature 02. Web feature 10 (approval card UI).

## Context To Read First

- `ai/project-kit/context/security-guardrails.md`

## Files Owned

- `ai/tools/write.ts` (proposal tools), shared proposal type in `ai/lib/proposals.ts`

## Files

CREATE: proposal schema (type, payload, targetRef); tools return proposals (never execute); expiry/conflict handling (re-validate before the app writes).

## Implementation Notes

- The agent NEVER performs the write - the app does. Deterministic writes (digest/reminders) skip approval by design.
- Window: proposal valid N minutes; stale proposals rejected by the UI.

## Separation of Concerns

- Autority stays with the backend + human; the agent only suggests.

## Docker & Deploy

- No change.

## Acceptance Criteria

- [ ] Proposed mutations appear as approval cards and write only after approval
- [ ] Stale/duplicate proposals handled


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Proposals carry org + role context (`organizationId`, effective role, `perms` keys from the JWT v2 claims) so the web approval card shows exactly who/where a write lands.
- Client-role sessions are restricted to feedback-shaped proposals only (comment/suggestion on their `ProjectClients`-attached projects); every other proposal type is rejected before the model composes it — the full client boundary lives in ai 13.
- Stale/expiry re-validation also re-checks active-org match: a proposal approved after an organization switch is stale and must be re-proposed (the token `org` claim changed).
- Custom roles (`custom:{roleId}`) are respected via the manifest/`perms` claim; the agent never infers permissions beyond what the token carries.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
