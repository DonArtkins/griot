# AI Architecture

## Positioning

`ai/` is a separate npm package (own `.nvmrc` -> 20, own lockfile) in the GTP repo. It orchestrates LLM + tool calls; data access is ONLY through the backend GraphQL with `GRIOT_SERVICE_TOKEN`.

```
ai/
├── agents.ts          # agent definitions (griotCopilot, digests, reminders)
├── tasks/             # scheduled tasks (dueReminders, sprintDigest, staleBoard, standupBuilder)
├── tools/             # GraphQL-backed tool functions
├── lib/graphql.ts     # typed GraphQL client (service token)
├── lib/budget.ts      # Redis token budgets + alarms
├── tests/             # golden transcripts (mocked LLM)
└── .env               # LLM keys ONLY here
```

## Data flow

Copilot prompt -> `ai/` agent -> (tool calls) -> backend GraphQL -> SQL Server -> results streamed to web via Trigger realtime.

## Boundary

- No DB credentials in `ai/`. No HTTP routes (`/api/webhooks/trigger` is the backend's, HMAC-verified).
- Web Copilot = human approval gate; the app performs writes.

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
