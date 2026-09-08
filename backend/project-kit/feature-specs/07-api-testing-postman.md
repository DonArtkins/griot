# Feature 07 — API Testing using Postman

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"API testing using Postman"**: a single Postman collection covering REST + GraphQL, environment-chained (`baseUrl`, `accessToken`, `refreshToken`), with assertions — reused headlessly as **Newman** in CI by the qa system.

## Dependencies

- Features 04–06 (routes + bulk + dashboard exist).
- Feature 08 (auth endpoints exist; collection auth flow needs them).

## Context To Read First

- `backend/project-kit/context/api-surface.md` (route list is the collection skeleton)
- `research/week-02-backend-api-development.md` §7

## Agent Skills To Use

- `qa/.agents/skills/newman-api/SKILL.md` (collection conventions + CI reuse)

## Files Owned

- `Postman/Griot.postman_collection.json`
- `Postman/gtp-2026.postman_environment.json`
- `Postman/README.md` (how to run, import, chain)

## Files

CREATE: collection with `REST` and `GraphQL` folders mirroring `api-surface.md`.
CREATE: environment template with `baseUrl`, `accessToken`, `refreshToken`.
CREATE: `Postman/README.md`.
RUN: import into Postman; run the full collection green.

## Setup / Initialization

```bash
cd backend && dotnet run --project src/Griot.Api &
# in Postman: import collection + environment, set baseUrl=http://localhost:PORT
# run folder-by-folder; GraphQL folder uses the same accessToken var
```

## Implementation Notes

- Auth flow pre-requisite: `register`/`login` stores `accessToken` + `refreshToken` in the environment (chained).
- GraphQL folder: same endpoint, queries/mutations, same assertions (`pm.response.to.have.jsonSchema` for key shapes).
- Assertions on 200/400/401/404 as appropriate; time assertions on dashboard < 500ms.

## Separation of Concerns

- The collection is a **contract artifact**: it mirrors `api-surface.md` and is the portability layer into qa's Newman CI — one tool covers REST + GraphQL.

## Docker & Deploy

- Runs against local compose (dev) and deployed Railway (qa/week-7); Newman executes it in CI (qa spec 06).

## Out of Scope

Load testing (k6, qa spec 09); E2E UI (Cypress, qa spec 07).

## Future Modifications

- qa spec 06 wires Newman into GitHub Actions; Week-7 manual cycles reuse the same collection.

## Acceptance Criteria

- [ ] Collection runs green locally; env chaining works (tokens auto-fill)
- [ ] Both REST + GraphQL folders exist and match `api-surface.md`
- [ ] Key contract responses have JSON schema assertions


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
