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


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Topology note: no new services — tenancy adds no container, port or volume; org scope rides the JWT v2 token through the existing api/mcp hops (mcp 04 note).
- `infra/scripts/healthcheck.sh` unchanged; optional two-org seeded smoke proving isolation through the full local stack, shared with qa 14.
- `.env.example` JWT v2 rows mirror infra 03 (`JWT__Key`, `SUPERADMIN__*`, `Organizations__RetentionDays`).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
