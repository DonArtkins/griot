# Feature 06 - Unit & Integration Testing: xUnit (.NET)

## Type

NEW FEATURE

## What This Delivers

The Week-6 step 5 deliverable for the backend: xUnit unit tests (services with mocked repos) + integration tests (WebApplicationFactory against the real SQL Server container), incl. the refresh-rotation replay race and bulk-update atomicity.

## Dependencies

- Backend features 04–06, 07 (REST + bulk routes and auth code — the Postman collection in backend 08 is a contract artifact, not code under test).

## Context To Read First

- `qa/project-kit/context/{test-pyramid,coverage-gate}.md`

## Agent Skills To Use

- `qa/.agents/skills/xunit-dotnet/SKILL.md`

## Files Owned

- `backend/tests/Griot.Tests/**`

## Files

CREATE: `TaskServiceTests`, `AuthApiTests` (refresh replay + rate limit), `WorkspaceServiceTests`, `BoardQueryIntegrationTests`, dashboard proc tests.

## Implementation Notes

- Integration tests boot the API via `WebApplicationFactory<Program>` against the compose-provided SQL Server.
- Coverage collected via XPlat.

## Acceptance Criteria

- [ ] `dotnet test` green; refresh replay + bulk atomicity covered
- [ ] Coverage artifact produced


## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
