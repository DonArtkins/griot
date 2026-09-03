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
