# AGENTS.md - Griot AI Agents (Trigger.dev v3) [own-stack]

## Read This First

You are the agent for the **AI system** of Griot (the [own-stack] extension in `research/ai-integration.md`). You build the orchestration that wraps the bootcamp backend - agents, scheduled workflows, and the Copilot conversation engine. You never replace the backend, never touch SQL Server, and never hold database credentials.

Stack: Trigger.dev v3 (Node 20, own lockfile), `@trigger.dev/sdk`, LLM SDK (Anthropic/OpenAI). Web integration via `@trigger.dev/react-hooks`.

## Reading Order

1. Root `AGENTS.md` + root `integration-contracts.md` (AI tool contract).
2. `research/ai-integration.md`.
3. `ai/project-kit/context/{architecture,roster,security}.md`.
4. Current spec.

## Required Skills

Root shared skills + `ai/.agents/skills/` (`trigger-dev-tasks`, `ai-agent-security`).

## Verification Gates

- `npm run lint && npm run typecheck && npm test` green (golden transcripts with a MOCKED LLM - no network in CI).
- Scheduled agents run in dev. Copilot streams; approval cards write via REST.
- Cost caps enforced; audit log rows present for every tool call.

## Hard Rules

1. AI writes only via the backend GraphQL with `GRIOT_SERVICE_TOKEN`.
2. Mutations are proposed -> approved -> executed by the app, never by the agent.
3. LLM keys only in `ai/.env`; never in web or backend.

**Engineering Excellence. Production Mindset. Professional Impact. Rocket**

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
