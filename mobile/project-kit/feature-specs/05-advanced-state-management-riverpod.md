# Feature 05 — Advanced State Management (Riverpod)

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"Advanced state management (Provider / Riverpod)"**: Riverpod providers for auth, board/task/UI state, and the status-change *picker* flow — the explicit-store model matching web's Zustand conventions.

## Dependencies

- Mobile features 03 + 04 (data hooks available).

## Context To Read First

- `mobile/project-kit/context/{state-and-data,code-standards}.md`

## Agent Skills To Use

- `mobile/.agents/skills/riverpod-state/SKILL.md`

## Files Owned

- `mobile/lib/features/*/providers/*.dart`

## Implementation Notes

- `authProvider` (session), `boardProvider` (read + status picker state), `notificationsProvider`.
- Status change = picker → `PATCH /api/tasks/{id}` with optimistic provider update + rollback.
- UI-only state (filter, panel visibility) in providers.

## Separation of Concerns

- Providers orchestrate data + UI state; screens consume; network stays in `core/`.

## Docker & Deploy

- No change.

## Out of Scope

Drag-drop (web-only).

## Acceptance Criteria

- [ ] Status picker updates a task and persists via REST
- [ ] Providers keep UI state + session state; no server-state duplication
