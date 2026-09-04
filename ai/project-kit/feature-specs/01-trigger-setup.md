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

```bash
mkdir -p ai && cd ai && nvm use
npx trigger.dev@3 login
npx trigger.dev@3 init --project-ref <PROJECT_REF>
npm i @trigger.dev/sdk @trigger.dev/react-hooks
npm i zod
```

**Version pinning (mandatory):** Record the exact `trigger.dev` CLI version installed in `ai/package.json` under `devDependencies` (e.g. `"trigger.dev": "3.x.y"`). All subsequent `deploy` and `dev` commands (DEPLOYMENT.md, RUNBOOK-ROLLBACK.md) use the pinned version — never `@latest`. Run `npx trigger.dev --version` after init and commit the result.

## Files Owned

- `ai/**` (scaffold + config)

## Files

CREATE: `ai/agents.ts` stub, `ai/tasks/health.ts` (runnable scheduled task), `ai/lib/graphql.ts` (client stub), `.env.example` (GRIOT_API_URL, GRIOT_SERVICE_TOKEN, ANTHROPIC_API_KEY/OPENAI_API_KEY).

## Implementation Notes

- Lock env so LLM keys never leave this folder (isolation contract).
- Tool manifest/lockfiles separate from web.

## Separation of Concerns

- AI orchestration only here; no DB, no HTTP routes.

## Docker & Deploy

- Deployed to Trigger cloud (or self-hosted Railway); dev via `npx trigger.dev dev`.

## Assignment / Acceptance Criteria

- [ ] `npx trigger.dev dev` runs the health task
- [ ] `.env.example` matches the ai rows in root `integration-contracts.md`
