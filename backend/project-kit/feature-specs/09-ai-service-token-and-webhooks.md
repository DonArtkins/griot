# Feature 09 — AI Service Token + Webhooks (own-stack)

## Type

NEW FEATURE (`[own-stack]` — the AI boundary defined in `research/ai-integration.md`)

## What This Delivers

The trusted AI-onboarding surface: `GRIOT_SERVICE_TOKEN` resolves to a dedicated `ai-agent` workspace principal with a reduced role (ReadWorkspace, CreateTask, AddComment, CreateNotification — no deletes, no invites), and `POST /api/webhooks/trigger` verifies Trigger.dev webhooks via HMAC so background runs can request non-LLM work.

## Dependencies

- Feature 07 (Bearer auth middleware exists to extend).

## Context To Read First

- `research/ai-integration.md` §7 (security & guardrails)
- `backend/project-kit/context/api-surface.md` (webhook route)

## Agent Skills To Use

- `ai/.agents/skills/ai-agent-security/SKILL.md` (scope/guardrail patterns)
- `backend/.agents/skills/jwt-argon2-auth/SKILL.md`

## Files Owned

- `backend/src/Griot.Api/Auth/ServiceTokenHandler.cs` (converts token → ai-agent principal)
- `backend/src/Griot.Api/Middleware/WebhookHmacMiddleware.cs`
- `backend/src/Griot.Api/Controllers/WebhookController.cs`

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
#   TRIGGER_WEBHOOK_SECRET=<from Trigger.dev>
```

## Implementation Notes

- `ai-agent` principal has a reduced role list and is workspace-scoped; the same policy code as any member.
- HMAC verified in middleware before any handler runs; failures return 401.
- The activity feed is the AI layer's audit + `summarize_project` source (no new tables).

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
- [ ] All AI tool calls are traceable to an ActivityLog row


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
