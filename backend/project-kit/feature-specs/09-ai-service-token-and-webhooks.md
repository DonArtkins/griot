# Feature 09 — AI Service Token + Webhooks (own-stack)

## Type

NEW FEATURE (`[own-stack]` — the AI boundary defined in `research/ai-integration.md`)

## What This Delivers

The trusted AI-onboarding surface, in **both directions** (orchestration contract: `research/ai-integration.md` §2a):

- **Trigger direction (.NET → Trigger.dev):** the backend enqueues AI tasks via Trigger.dev's REST API (server-to-server `TRIGGER_SECRET_KEY`) whenever something async or AI-related is needed — after validating/persisting the request. The backend is the only trigger source for request-originated work; the one exception is Trigger.dev's own cron scheduler, which starts the scheduled agents (`dueReminders`, `sprintDigest`, `staleBoard`, `standupBuilder`) with no backend involvement (orchestration contract §2a).
- **Callback direction (Trigger.dev → .NET):** `POST /api/webhooks/trigger` verifies Trigger.dev webhooks via HMAC (`X-Trigger-Signature`) so background runs write results back through the API.
- **Service token (AI → .NET data plane):** `GRIOT_SERVICE_TOKEN` resolves to a dedicated `ai-agent` workspace principal with a reduced role (ReadWorkspace, CreateTask, AddComment, CreateNotification — no deletes, no invites).

.NET remains the **only writer of source-of-truth data**; Trigger.dev is a compute/orchestration adapter, never a data owner. Web/mobile never trigger or poll Trigger.dev for task triggering or status — they call this API. Sole exception: the web Copilot panel consumes Trigger.dev's realtime WS as a scoped, read-only streaming delivery channel (no triggering, no status polling); mobile has no such exception.

## Dependencies

- Feature 07 (Bearer auth middleware exists to extend).

## Context To Read First

- `research/ai-integration.md` §2a (orchestration contract) + §7 (security & guardrails)
- `backend/project-kit/context/api-surface.md` (webhook route)

## Agent Skills To Use

- `ai/.agents/skills/ai-agent-security/SKILL.md` (scope/guardrail patterns)
- `backend/.agents/skills/jwt-argon2-auth/SKILL.md`

## Files Owned

- `backend/src/Griot.Api/Auth/ServiceTokenHandler.cs` (converts token → ai-agent principal)
- `backend/src/Griot.Api/Middleware/WebhookHmacMiddleware.cs`
- `backend/src/Griot.Api/Controllers/WebhookController.cs`
- `backend/src/Griot.Infrastructure/Integrations/TriggerDevClient.cs` (enqueue tasks by ID via Trigger.dev REST)

## Files

CREATE: service-token auth handler (constant-time compare of `GRIOT_SERVICE_TOKEN`; builds `ai-agent` claims incl. `wid` scope).
CREATE: HMAC middleware verifying `X-Trigger-Signature` against the Trigger webhook secret.
CREATE: `WebhookController` — `POST /api/webhooks/trigger` (relay to the job/routing logic).
MODIFY: `Program.cs` — register both.
RUN: `dotnet build`; test with a signed webhook fixture.

## Setup / Initialization

```bash
# env additions:
#   GRIOT_SERVICE_TOKEN=<long-random>       (shared with ai/mcp env)
#   TRIGGER_SECRET_KEY=<server-to-server>   (backend → Trigger.dev REST; NEVER exposed to web/mobile)
#   WEBHOOK_SECRET=<from Trigger.dev>       (Trigger.dev → backend HMAC callbacks; read by
#                                           WebhookController as Webhook:Secret ?? WEBHOOK_SECRET)
```

## Implementation Notes

- `ai-agent` principal has a reduced role list and is workspace-scoped; the same policy code as any member.
- HMAC verified in middleware before any handler runs; failures return 401.
- The activity feed is the AI layer's audit + `summarize_project` source (no new tables).
- Enqueue-after-persist: a request that needs AI work is validated/persisted first, then `TriggerDevClient` enqueues the task by ID. If the enqueue fails, the domain write stands (AI is a post-processing adapter, not part of the transaction).
- Task results come back as HMAC-verified webhook calls or service-token REST writes — the backend stays the single writer of source-of-truth data.

## Separation of Concerns

- Auth policy = `Griot.Api` concern; the token is validated at the boundary, policy enforced by the same services any member uses. AI code stays out of the backend solution entirely.

## Docker & Deploy

- Env keys added to compose (infra) + Railway (infra spec 05) + `ai/mcp` environments. No new container.

## Out of Scope

AI scheduling, tool execution, LLM calls (all in `ai/`/`mcp/` systems).

## Future Modifications

- ai spec 03 + mcp spec 02 consume this surface; qa spec 11 audits the AI principal scope.

## Acceptance Criteria

- [ ] Service token resolves to the restricted ai-agent principal; deletes/invites rejected
- [ ] Webhook HMAC verified; bad signatures 401
- [ ] `TriggerDevClient` enqueues a task by ID with `TRIGGER_SECRET_KEY`; enqueue failure does not roll back the domain write
- [ ] Web/mobile never receive any Trigger.dev credential (no `TRIGGER_SECRET_KEY` outside backend env)
- [ ] All AI tool calls are traceable to an ActivityLog row


## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
