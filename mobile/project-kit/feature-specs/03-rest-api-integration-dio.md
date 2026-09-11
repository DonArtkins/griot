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


## Multi-Tenant Update (2026-09-11 — PLANNED)

- The dio interceptor keeps sending the Bearer access token; the server resolves tenant context from the JWT `org` claim — never from a caller-supplied header or body (backend 29).
- Handle the `403 org_suspended` error code: surface a suspended-organization state (switch org or re-login path) instead of a generic failure toast.
- On organization switch (`POST /api/auth/select-organization`), the response re-issues the token pair — atomically replace both tokens (memory access + secure-storage refresh) and cancel in-flight requests.
- New typed endpoints mirror the routes PLANNED in backend 29–35 (`/api/organizations`, `/api/auth/select-organization`, client portal + `ClientFeedback` routes); DTOs come from spec 01 (`OrganizationDto`, `ClientProjectViewDto`, `ClientFeedbackDto`).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
