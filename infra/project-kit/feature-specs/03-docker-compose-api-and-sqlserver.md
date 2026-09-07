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
