# Feature 04 - Build & Orchestrate the Full Topology

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable "Build and orchestrate services": the complete local pipeline matching production - `api`, data stores, and the `mcp` service - started, health-checked, and exercised.

## Dependencies

- Feature 03 (compose core).
- mcp feature 01 (server runnable).

## Context To Read First

- `infra/project-kit/context/deployment-targets.md`

## Agent Skills To Use

- `infra/.agents/skills/docker-compose/SKILL.md`

## Files Owned

- `docker-compose.yml` (add `mcp`), `infra/scripts/healthcheck.sh`

## Files

MODIFY: compose - add `mcp` service (Streamable HTTP 3001:3001, `GRIOT_API_URL`/`GRIOT_SERVICE_TOKEN` env).
CREATE: `infra/scripts/healthcheck.sh` (curl api `/health` + mcp; redis ping; sql probe).
RUN: full `docker compose up -d`; run healthcheck; smoke web -> api -> SQL Server.

## Implementation Notes

- Local parity rule: what runs here deploys (feature 06). Document the `docker compose ps` matrix in the runbook.

## Separation of Concerns

- Orchestration stays in infra; each service image is owned by its system's Dockerfile.

## Docker & Deploy

- This topology is the reference for Railway (06) and CI (05).

## Out of Scope

CI (05), cloud hosts (06).

## Acceptance Criteria

- [ ] Full stack up + healthy in one command
- [ ] End-to-end smoke passes (web -> api -> SQL Server)
