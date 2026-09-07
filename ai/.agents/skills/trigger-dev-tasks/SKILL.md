---
name: trigger-dev-tasks
description: "Trigger.dev v3 tasks/agents on Griot: durable scheduled jobs, streaming to the web Copilot via realtime, idempotency, and webhook HMAC to the backend."
metadata:
  version: "0.1.0"
---

# Trigger.dev Tasks Skill

## Setup

```bash
cd ai && npx trigger.dev@latest login
npx trigger.dev@latest init --project-ref <PROJECT_REF>
npm i @trigger.dev/sdk @trigger.dev/react-hooks
```

## Patterns

- Agents defined in TS (`agents.ts`) using `@trigger.dev/sdk/v3` agent API; tool calls go to the backend GraphQL with `GRIOT_SERVICE_TOKEN`.
- Scheduled tasks: `dueReminders`, `sprintDigest`, `staleBoard`, `standupBuilder` - durable, idempotent runs.
- Real-time streaming to web via `useRealtimeRun`/`useRealtimeStream` in `@trigger.dev/react-hooks`.
- NEVER wrap `triggerAndWait`/`batchTriggerAndWait` in `Promise.all`/
- Webhook: backend verifies HMAC `X-Trigger-Signature` for `POST /api/webhooks/trigger`.

## Rules

- No DB access; no direct LLM keys in web. LLM keys only in `ai/.env`.
- Every agent run logs tool calls (workspaceId, tool, payloadHash, runId) for the audit trail.
