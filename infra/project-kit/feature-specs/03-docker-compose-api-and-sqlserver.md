# Feature 03 - Docker Compose (API + SQL Server)

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable exactly as the PDF names it: "Create docker-compose.yml (API + SQL Server)" - plus Postgres (secondary) and Redis (auth support) per the stack.

## Dependencies

- Feature 02 (API image builds).
- Real Docker Engine (`docker --version` not podman).

## Context To Read First

- `research/week-05-deployment-devops.md` sec 3
- `research/gtp-2026-prep.md` sec 6.4 (sababisha-* naming + ports)

## Agent Skills To Use

- `infra/.agents/skills/docker-compose/SKILL.md`

## Files Owned

- root `docker-compose.yml`
- `.env.example` (compose rows)

## Files

CREATE: `docker-compose.yml` - services `api`, `sababisha-sqlserver` (2022-latest), `sababisha-postgres` (16-alpine), `sababisha-redis` (7-alpine); ports 14333:1433, 5433:5432, 6380:6379 host; `8080:8080` API; volumes `sababisha_mssql`/`sababisha_pg`; `depends_on`; health checks.

RUN: `docker compose up -d`; verify all healthy.

## Implementation Notes

- Connection string env-injected into `api` (`ConnectionStrings__Default` = `Server=sababisha-sqlserver,1433;Database=griot;...`).
- Health checks for `api` + `sqlserver`.

## Separation of Concerns

- Compose is dev parity + prod reference; secrets via `.env`; data in named volumes.

## Docker & Deploy

- `docker compose up` == the full local topology.

## Out of Scope

`mcp` service (feature 04), CI (05).

## Acceptance Criteria

- [ ] `docker compose up -d` brings all services healthy
- [ ] SQL Server reachable (DBeaver 14333); Redis PONG (6380)


## Tri-agent verification note (2026-09-10)

The infrastructure review that produced backend specs 20/21 (`docs/observability/LOGGING-AUDIT-REPORT.md`,
ADR-004) requires a compose-side change owned HERE: a **backup sidecar service** (opt-in
`profile: backup`) writing SQL Server FULL/DIFF/LOG backups to a new named volume
`sababisha_mssql_backup` — the "copies of the database" guarantee. When this spec is
implemented, add: the sidecar service + volume, `.env.example` rows, and health-check
parity. Until then this note is the planned-behavior marker (not implemented evidence).
Spec 21 owns the T-SQL; this spec owns the container + volume.

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
