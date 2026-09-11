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

**Resolve the Trigger.dev version contract FIRST** — the current pre-scaffold `ai/package.json` pins `trigger.dev` **4.0.0** + `@trigger.dev/sdk` `^4.0.0` (and `ai/package.json`'s description says "v4"), while the architecture, examples and `stack-contract.md` describe **v3**. Resolve a supported compatible set, update `ai/package.json` **and** `ai/package-lock.json` (never the manifest alone), and update `stack-contract.md` + research if adopting v4. Only after the version is resolved do the login/init commands below use the pinned CLI. Do NOT run the bootstrap before this resolution.

```bash
mkdir -p ai && cd ai
npm ci   # installs the RESOLVED pinned CLI, not a drifted one
npm exec -- trigger --version   # commit the result — must match the pinned version
npm exec -- trigger login
npm exec -- trigger init --skip-package-install --project-ref <PROJECT_REF>
# Resolve/review compatible exact SDK/react-hooks/zod versions before adding them.
```

**Version pinning (mandatory):** Record the exact `trigger.dev` CLI version installed in `ai/package.json` under `devDependencies`. All subsequent `deploy` and `dev` commands (DEPLOYMENT.md, RUNBOOK-ROLLBACK.md) use the pinned version — never `@latest`. No package upgrade is performed by this planning audit; the version resolution above is the implementation entry gate per the review finding on the v3/v4 discrepancy.

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


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
