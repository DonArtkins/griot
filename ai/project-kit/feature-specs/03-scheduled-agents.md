# Feature 03 - Scheduled Agents (Reminders, Digest, Stale, Standup)

## Type

NEW FEATURE ([own-stack])

## What This Delivers

The four scheduled workflows: `dueReminders` (daily), `sprintDigest` (weekly), `staleBoard` (daily), `standupBuilder` (Monday) - durable, idempotent, observable from the Trigger dashboard.

## Dependencies

- Feature 02 (tooling). Backend notifications route exists.

## Context To Read First

- `ai/project-kit/context/roster.md`

## Agent Skills To Use

- `ai/.agents/skills/trigger-dev-tasks/SKILL.md`

## Files Owned

- `ai/tasks/{dueReminders,sprintDigest,staleBoard,standupBuilder}.ts`

## Files

CREATE: 4 scheduled tasks with cron schedules; deterministic paths skip human approval; digests/reminders create notifications via REST/GraphQL.

## Implementation Notes

- Idempotency keys per run; staging in the Trigger dashboard.
- `staleBoard` uses `TaskItems(DueDate)`/column durations; digest uses `ActivityLogs`.

## Separation of Concerns

- Scheduling + generation here; delivery (notifications) via backend API.

## Docker & Deploy

- Trigger cloud schedules; dashboard observability.

## Acceptance Criteria

- [ ] All four run on schedule in dev and produce expected outputs
- [ ] Idempotent (no duplicate notifications on retry)


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Per-org scheduling: each scheduled run fans out per active organization, resolving that org's OBO identity from the backend job; every output (notifications, digests) is org-scoped (backend 22 bump keys fan-out per org).
- Tenant-aware budgets: token budget keys partition per organization (`budget:org:{orgId}:…`) in addition to the existing per-workspace/day guard, so one tenant cannot exhaust the shared daily budget.
- Scheduled digests never mix organizations: a run computes and emits within a single org's data; fan-out across orgs is one job iteration per org.
- Idempotency keys become org-stamped (`org:{orgId}:…`) so a retry can never leak output into another tenant.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
