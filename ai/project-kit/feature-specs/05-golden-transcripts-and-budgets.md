# Feature 05 - Golden Transcripts + Token Budgets

## Type

NEW FEATURE ([own-stack])

## What This Delivers

Deterministic CI tests (golden transcripts with a mocked LLM) and the Redis-backed daily token budget with alarms - the cost/quality guardrails.

## Dependencies

- Features 01-04 (agents/tools exist).

## Context To Read First

- `ai/project-kit/context/security-guardrails.md`

## Files Owned

- `ai/tests/**`, `ai/lib/budget.ts`

## Files

CREATE: golden-transcript fixtures (`(conversation, mocked LLM outputs) -> expected tool-call order`); budget guard (Redis INCR per workspace/day, alarm over budget); CI job (`npm test` no network).

## Implementation Notes

- Inject the LLM client at construction; assert exact tool-call sequences.
- Cost caps + alarms observable; over-budget runs fail fast.

## Separation of Concerns

- Tests live in the ai package; the budget is an infrastructure concern (Redis) used by the ai code.

## Docker & Deploy

- CI job wired into infra feature 05 (`test-ai`).

## Acceptance Criteria

- [ ] Golden transcripts green in CI with a mocked LLM
- [ ] Budget enforced per workspace; alarms fire on over-budget


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Add multi-tenant transcript suites: two-org fixtures proving no cross-tenant data leakage (org A sessions never receive org B rows), suspended-org fixtures (writes rejected, reads scoped), and org-switch mid-thread fixtures (post-switch context is rebuilt from the new token `org` claim).
- Add client-boundary suites: a `Client`-role session never receives internal board internals, raw logs, other tenants' data, or internal capability names — asserted at the tool-call level, not in prose.
- Custom-role fixtures: `custom:{roleId}` permissions resolve exactly from the token `perms` claim in transcripts.
- Budget fixtures extend to per-org partitioning (`budget:org:{orgId}:…`); over-budget in one org does not consume another org's allowance.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
