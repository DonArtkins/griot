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
4. **Trigger.dev is a compute/orchestration adapter, never a data owner.** Every result is written back by calling the .NET API (`POST /api/webhooks/trigger` HMAC or service-token REST); `ai/` never writes to SQL Server, never holds DB credentials.
5. **`ai/` tasks are triggered only by the .NET backend** (Trigger.dev REST/SDK, server-to-server `TRIGGER_SECRET_KEY`). Web/mobile never trigger or poll Trigger.dev — they call the .NET API, which enqueues tasks. The only direct web↔Trigger channel is the Copilot realtime stream (scoped access token, read-only delivery).
6. Scheduled agents (`dueReminders`, `sprintDigest`, `staleBoard`, `standupBuilder`) run on Trigger.dev schedules but persist their output through the same .NET-only path.

See `research/ai-integration.md` §2a for the authoritative orchestration contract.

**Engineering Excellence. Production Mindset. Professional Impact. Rocket**

## Implemented authentication contract (Feature 07)

Use the [auth contract](../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

Before committing or pushing implementation, run `python3 scripts/check-contract-sync.py` from
the repository root. Synchronize the owning spec, dependent specs, planning,
research, docs, contexts, agent instructions, diagram sources and progress notes
in the feature branch. Planned behavior must be labeled and must not count as
implemented acceptance evidence. Run the system verification gates as well.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
