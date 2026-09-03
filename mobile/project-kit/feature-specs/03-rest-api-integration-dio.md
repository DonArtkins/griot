# Feature 03 — REST API Integration (dio)

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"REST API integration"**: the dio client with interceptors (401→refresh→retry once), typed API methods for auth/workspace/project/task/comment/notification/upload, and the first feature hooks.

## Dependencies

- Mobile feature 02 (auth provider for the Bearer).

## Context To Read First

- `mobile/project-kit/context/api-integration.md`

## Agent Skills To Use

- `mobile/.agents/skills/dio-rest/SKILL.md`

## Files Owned

- `mobile/lib/core/network/dio_client.dart` (+ interceptor)
- `mobile/lib/core/network/api/*.dart` (typed endpoints)

## Implementation Notes

- Base URL from `String.fromEnvironment('API_URL')`.
- RefreshInterceptor mirrors the web Axios behavior; `_retried` guard against loops.
- Model classes (DTOs) mirror the backend DTO contract.

## Separation of Concerns

- `core/network` = transport; feature providers consume typed APIs.

## Docker & Deploy

- No change.

## Out of Scope

GraphQL (feature 04).

## Acceptance Criteria

- [ ] Typed REST client works vs the backend; interceptor refresh verified
- [ ] DTO models match the backend contract
