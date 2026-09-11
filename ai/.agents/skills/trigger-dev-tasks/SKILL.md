---
name: trigger-dev-tasks
description: "Trigger.dev v4 tasks/agents on Griot: durable scheduled jobs, streaming to the web Copilot via realtime, idempotency, and webhook HMAC to the backend."
metadata:
  version: "0.1.0"
---

# Trigger.dev Tasks Skill

## Setup

```bash
cd ai && npx trigger.dev@latest login
npx trigger.dev@latest init --skip-package-install --project-ref <PROJECT_REF>
npm i @trigger.dev/sdk @trigger.dev/react-hooks
```

## Patterns

- Trigger tasks use `@trigger.dev/sdk` v4; verify the orchestration/LLM library API in Context7 before implementing agent loops. Do not assume Trigger exports `agent` or `tool`. Data calls use backend REST/GraphQL with the trusted OBO delegation described by ai-agent-security.
- Scheduled tasks: `dueReminders`, `sprintDigest`, `staleBoard`, `standupBuilder` - durable, idempotent runs.
- Real-time streaming to web via `useRealtimeRun`/`useRealtimeStream` in `@trigger.dev/react-hooks`.
- NEVER wrap `triggerAndWait`/`batchTriggerAndWait` in `Promise.all`/
- Webhook: backend verifies HMAC `X-Trigger-Signature` for `POST /api/webhooks/trigger`.

## Rules

- No DB access; no direct LLM keys in web. LLM keys only in `ai/.env`.
- Every agent run logs tool calls (workspaceId, tool, payloadHash, runId) for the audit trail.
