# Feature 01 - Trigger.dev Project Setup (ai/)

## Type

NEW FEATURE ([own-stack])

## What This Delivers

The `ai/` npm package initialized and connected to a Trigger.dev project: toolbox, `.nvmrc` (20), env with LLM keys limited to `ai/.env`, and a dummy runnable task proving the pipeline.

## Dependencies

- Backend feature 09 (service token + webhooks) for the eventual wiring; not blocking scaffold.
- Node 20 per-project.

## Context To Read First

- `research/ai-integration.md` sec 2
- `ai/project-kit/context/{architecture,code-standards}.md`

## Agent Skills To Use

- `ai/.agents/skills/trigger-dev-tasks/SKILL.md`

## Setup / Initialization

**Resolved contract:** Trigger.dev v4 [own-stack], preserving the v4 adoption in commit `7e90c5b`. Pin `trigger.dev`, `@trigger.dev/sdk` and `@trigger.dev/react-hooks` to **4.5.16** in both manifest and lockfile. Use the repository's Node 20 toolchain. The [official migration notice](https://trigger.dev/docs/migrating-from-v3) retires v3 deployments on April 1, 2026 and shuts v3 down on July 1, 2026; downgrading to v3 cannot bootstrap this cloud-only system. Architecture/research references to v3 were stale. Context7 confirms the v4 import is `@trigger.dev/sdk`.

```bash
mkdir -p ai && cd ai
npm ci   # installs the RESOLVED pinned CLI, not a drifted one
npm exec -- trigger --version   # commit the result — must match the pinned version
npm exec -- trigger login
npm exec -- trigger init --skip-package-install --project-ref <PROJECT_REF>
# Resolve/review compatible exact SDK/react-hooks/zod versions before adding them.
```

**Version pinning:** Dev/deploy use the installed CLI through package scripts. Review any generated manifest/lockfile diff after init; it must preserve the exact compatible package set. Login, project creation, task implementation and deployment remain pending AI 01; package synchronization is not evidence those acceptance criteria passed.

## Files Owned

- `ai/**` (scaffold + config)

## Files

CREATE: `ai/agents.ts` stub, `ai/tasks/health.ts` (runnable scheduled task), `ai/lib/graphql.ts` (client stub), `.env.example` (GRIOT_API_URL, GRIOT_SERVICE_TOKEN, ANTHROPIC_API_KEY/OPENAI_API_KEY).

## Implementation Notes

- Lock env so LLM keys never leave this folder (isolation contract).
- Tool manifest/lockfiles separate from web.
- The planned GraphQL client accepts an authorized real-user ID from trusted task context and sends `X-On-Behalf-Of` with Bearer `GRIOT_SERVICE_TOKEN`; scheduled runs follow the same rule. Missing identity must fail closed. Backend 09 uses real-user OBO (`ai-on-behalf-of`, exactly four scopes), not a synthetic AI member.

## Separation of Concerns

- AI orchestration only here; no DB, no HTTP routes.
- Standalone service: deployed independently (Trigger cloud) and triggered only by the .NET backend (research/ai-integration.md §2a). Tasks never write domain data directly — write-back goes through the .NET API (webhook HMAC or service-token REST).

## Docker & Deploy

- Deployed to Trigger cloud only; local dev via `npm run dev` (the package script uses the exact installed CLI). Self-hosted control planes are out of scope.

## Assignment / Acceptance Criteria

- [ ] `npm run dev` runs the health task with the pinned CLI
- [ ] `.env.example` matches the ai rows in root `integration-contracts.md`


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Task payloads and events become org-stamped: every task invocation resolves the OBO principal **and** the active `org` claim from the backend job context (backend 29/30) and records `organizationId` in run metadata and the payload envelope.
- Run metadata/observability (Trigger dashboard tags, completion callbacks) carries the org stamp so per-tenant tracing is possible once backend logs gain `OrganizationId`.
- The OBO delegation becomes org-stamped: `ServiceToken:Delegations:{userId}` grants are scoped per organization (backend 29–33 wave); the GraphQL client passes the delegation's org through the headers it already sends and never invents an org value.
- Scheduled runs and HMAC webhook callbacks must **fail closed** when no org context is resolvable from the backend job — no default-tenant fallback.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
