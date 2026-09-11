# Feature 04 - Container + Railway Deployment

## Type

NEW FEATURE ([own-stack]; joint with infra)

## What This Delivers

The MCP server as a Docker image running **Streamable HTTP** mode, deployed to Railway beside the backend API - the same topology the web Copilot and external clients use.

## Dependencies

- Feature 03 (real client). Infra features 02-04/06 (compose + Railway).

## Context To Read First

- `mcp/project-kit/context/architecture.md` sec Transports

## Agent Skills To Use

- `infra/.agents/skills/docker-compose/SKILL.md`, `infra/.agents/skills/railway-hosting/SKILL.md`

## Files Owned

- `mcp/Dockerfile`, compose `mcp` service (with infra)

## Files

CREATE: `mcp/Dockerfile` (node:20-alpine runtime, run Streamable HTTP on 3001).
MODIFY: compose + Railway runbooks - `mcp` service env `GRIOT_API_URL`, `GRIOT_SERVICE_TOKEN`.

## Implementation Notes

- stdio mode stays available locally for Claude/Cursor/Cline.
- Health check on the transport endpoint.

## Separation of Concerns

- Image owned by mcp; orchestration by infra; deploy by CI.

## Docker & Deploy

- Railway service alongside the API; reachable by external clients + web.

## Acceptance Criteria

- [ ] Container runs Streamable HTTP; health OK
- [ ] Railway deployment reachable; `claude mcp add` (remote) connects


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Env unchanged: `GRIOT_API_URL` + `GRIOT_SERVICE_TOKEN` remain the only MCP container env; the active org is a per-session property of the delegated principal — never an env var, container property or Railway variable.
- Contract note: Streamable HTTP sessions bind user + workspace + active org; stdio profiles bind the operator-configured user/org.
- No new container, port, service or compose change (infra 03/04 topology untouched); contract-sync only when backend 29/30 land.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
