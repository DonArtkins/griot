# AI Architecture

## Positioning

`ai/` is a separate npm package (own `.nvmrc` -> 20, own lockfile) in the GTP repo. It orchestrates LLM + tool calls; **domain reads and tool access are ONLY through the backend GraphQL with `GRIOT_SERVICE_TOKEN`.** The one REST exception is the required write-back: task results return to the .NET API via `POST /api/webhooks/trigger` (HMAC) or service-token REST — .NET remains the only writer of source-of-truth data.

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

The planned shared API client must accept the authorized real user's ID for each
call and send `X-On-Behalf-Of` alongside Bearer `GRIOT_SERVICE_TOKEN`. Obtain the
identity from trusted execution context, never model-generated text. This applies
to scheduled runs and service-token REST write-back as well as GraphQL. Backend
spec 09 issues role `ai-on-behalf-of` and exactly four scopes:
ReadWorkspace/CreateTask/AddComment/CreateNotification; it creates no synthetic
workspace member. Client implementation remains planned for this system's own branch.

Copilot prompt -> `ai/` agent -> (tool calls) -> backend GraphQL -> SQL Server -> results streamed to web via Trigger realtime.

## Orchestration contract (research/ai-integration.md §2a — authoritative)

- **Standalone service:** `ai/` is a separate Node/TS project deployed independently on **Trigger cloud** (dev runs go through `npx trigger.dev dev`, which tunnels to the cloud control plane). Self-hosted Docker control planes are NOT part of the contract — they would require an extra `TRIGGER_API_URL` pointing at the self-hosted API and are unimplemented/out of scope (revisit only if Trigger cloud is ever dropped). This is where AI calls and long-running background work execute.
- **.NET orchestrates:** the backend triggers tasks via Trigger.dev's REST API/SDK (`POST` by task ID against the Trigger cloud API, authenticated with `TRIGGER_SECRET_KEY`) whenever something async or AI-related is needed. The backend validates/persists the request FIRST, then enqueues. Scheduled agents are the one exception: Trigger.dev's own cron scheduler starts them (see `ai/AGENTS.md` rule 6 / `research/ai-integration.md` §2a).
- **Write-back:** every task result is written back by calling the .NET API (`POST /api/webhooks/trigger`, HMAC-verified, or service-token REST). **Trigger.dev never owns domain data** — .NET is the only writer of source-of-truth data.
- **Frontend isolation:** web and mobile never touch Trigger.dev for triggering or status — they call the .NET API, which internally enqueues tasks, and poll .NET / use SignalR for status. Sole exception: the web Copilot panel consumes Trigger's realtime WS (scoped access token) as a read-only streaming delivery channel.
- **Server-to-server secrets only:** `TRIGGER_SECRET_KEY` (backend → Trigger) and `WEBHOOK_SECRET` (backend config `Webhook:Secret`; Trigger.dev → backend HMAC callbacks) never reach the frontend or mobile.
- **Why TS/Trigger.dev for AI:** TS-first AI SDKs (OpenAI/Anthropic/Vercel AI SDK/LangChain.js), built-in retries/concurrency/waitpoints for slow-streaming-retryable AI calls, and it keeps prompt orchestration + model-provider churn out of the domain API.

## Boundary

- No DB credentials in `ai/`. No HTTP routes (`/api/webhooks/trigger` is the backend's, HMAC-verified).
- Web Copilot = human approval gate; the app performs writes.

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.


**Superpowers (specs 06–08, PLANNED):** the Copilot gains a knowledge agent + system auditor (ai 06, read-only, RBAC-scoped, cites sources), report generation to PDF/CSV (ai 07, deterministic aggregation in Node, artifacts via backend 11/24), and a Level-4 planning executor (ai 08, PLAN → human gate → ACT → OBSERVE, full-plan approval + idempotency). All through `GRIOT_SERVICE_TOKEN` + `X-On-Behalf-Of`; NEW scoped capability `CreateReport` (backend 24) for report rows; NEVER auth/OTP/delete/invite/member tools.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
