# Feature 08 — API Testing using Postman

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"API testing using Postman"**: a single Postman collection covering REST + GraphQL, environment-chained (`baseUrl`, `accessToken`, `refreshToken`), with assertions — reused headlessly as **Newman** in CI by the qa system.

## Dependencies

- Features 04–06 (routes + bulk + dashboard exist).
- Feature 07 (auth endpoints exist; collection auth flow needs them).

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

- Runs against local compose (dev) and deployed Railway (qa/week-7); Newman executes it in CI (qa spec 05).

## Out of Scope

Load testing (k6, qa spec 10); E2E UI (Cypress, qa spec 09).

## Future Modifications

- qa spec 05 wires Newman into GitHub Actions; Week-7 manual cycles reuse the same collection.

## Acceptance Criteria

- [x] Collection runs green locally; env chaining works (tokens auto-fill) — Register/Login Tests scripts write `accessToken`/`refreshToken`/`userId` to env; GraphQL + later REST folders read `{{accessToken}}` via `{{authHeader}}`; scaffold assertions tolerate 501 pending later specs.
- [x] Both REST + GraphQL folders exist and match `api-surface.md` — REST contains 12 module folders (Public, Auth, Workspaces, Projects, Boards&Columns, Tasks, Comments, Attachments, Notifications, Dashboard&Logs, Webhooks) with every route listed in `api-surface.md` §REST; GraphQL contains 11 queries + 3 mutations + SDL endpoint matching `api-surface.md` §GraphQL (boardId-required tasks, per-resolver workspace auth documented).
- [x] Key contract responses have JSON schema assertions — Register/Login/Refresh use `pm.response.to.have.jsonSchema` for token pair + user shape; X-Request-Id asserted on every REST request; 429→Retry-After on Login/OTP; dashboard latency <500 ms asserted on both REST dashboardSummary and GraphQL.


## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

## Postman folder 14 — Audit (added 2026-09-10, syncs with specs 20–22)

The collection gained folder **"14. Audit"** (`backend/Postman/Griot.postman_collection.json`):
logs read-guards (`/api/logs/errors`, `/api/logs/audit` — 200-or-403 tolerant),
an audit-trail capture assertion keyed on `{{taskId}}` (tolerant until backend spec 20
ships, then tightened to assert non-empty rows), and a `POST /api/notifications/fanout`
gate probe (401/403/404 tolerant until spec 22 builds the route). Newman (qa 05) reuses
this collection unchanged. This folder is part of the spec-08 deliverable surface; its
strict assertions activate as specs 18–22 land — labeled planned-vs-implemented.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
