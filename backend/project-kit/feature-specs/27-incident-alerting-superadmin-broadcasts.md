# Backend Feature 27 — Incident Alerts and Confirmed Notices [own-stack]

**Status:** PLANNED. Automatic incident alerts and human-directed outbound notices have separate authorization rules. One feature branch: `feature/backend/27-incident-alerting-superadmin-broadcasts`. (Backend spec 12 is already implemented; the DKIM/DMARC doc correction shipping in this planning wave is not a spec-12 implementation and does not re-open that feature.)

## Type

New orchestration over existing logging, durable jobs, in-app notifications and Brevo email.

## Dependencies

Backend 09, 12 (email), 19 (limits), 20 (durable outbox/inbox and audit), 22 (fan-out), 23 (human step-up), 25 (platform tiers). Ai 10 and web 12 are later consumers; backend tests use deterministic drafts and do not wait for those systems.

## Context To Read First

Logging runtime, communication guide, ai 10, `research/ai-integration.md`, proposed ERD amendments. Use contract-sync, dotnet-ef-core and Context7.

## Files Owned

Application incident/notice orchestration, Infrastructure delivery/job adapters and proposed BroadcastDraft persistence, Api draft/confirm/cancel/status controllers, SQL and Postman tests.

## Setup / Initialization

Approve the proposed draft/job/audience schema before migration. Reuse spec 20's durable jobs and spec 22's per-channel delivery records. A draft records creator, exact copy/channel set, immutable recipient IDs/hash, resolved count, UTC send/reminder instants, display timezone, revision and status. UI time defaults to Africa/Nairobi but stores UTC. No new notification vendor, scheduler service or event bus.

## A. Automatic incident alerts

Persist ErrorLogs, then emit the alert event via the durable outbox. Do not perform an LLM/Trigger network call on the exception response path. Backend deterministic thresholds decide whether to alert: defaults one 5xx = log only; five 5xx in five minutes for one route = alert; an unhandled auth-route exception = immediate alert. Configuration `Alerts:Thresholds` can tune these values with an audit record. No payment monitoring: Griot has no payment module.

Dedupe by incident signature/window, cooldown 15 minutes, and send a recovery update on resolution. Resolve a fixed operator-provisioned SuperAdmin audience; no model-supplied recipients. Ai 10 may summarize only the permitted incident snapshot; model failure falls back to a deterministic summary. Counts cite coverage gaps because telemetry can be dropped. Root cause is a hypothesis unless supported by evidence.

Create in-app and email delivery intents together in one database transaction. Dispatch independently from the same job; provider delivery times cannot be atomic or simultaneous. Retry failed channels without duplicating successful channels. Incident metadata/audience/job result is auditable. Automatic alerts require no per-alert confirmation because this fixed policy was authorized in advance.

## B. Previewed, confirmed notices

SuperAdmin can draft maintenance/operational notices to `all_users`, `all_admins` or a chosen workspace. Workspace Owner/Admin can draft `role_check` reminders for their own workspace members only. A role-check reminder is a notification, not a formal report, and never changes permissions. In-app is the default; email is optional and shown in the preview. Role diffs come from authorized audit data since the previous notice; if unavailable, say unavailable and send only the generic reminder.

1. Backend resolves recipients and shows copy, channels, exact count/scope, send time and optional reminder time. Ai 10 writes copy only; no send capability.
2. Human confirms the exact draft revision and audience hash. Membership, authority, content or schedule changes invalidate confirmation and require a new preview; no silent audience expansion.
3. Commit approval, AuditLogs/ActivityLogs and channel job intents together. One-off T-1-hour reminders use delayed Trigger tasks; recurring cron is reserved for recurring schedules. Brevo and Trigger calls happen after commit.
4. Workers recheck cancellation/current authority and recipient eligibility. Newly added recipients require new confirmation; removals are suppressed. Use per-recipient/channel/event idempotency, retries and provider delivery status. Honor the account email budget and suppression rules; never claim the OTP window makes Brevo's daily cap unreachable.
5. Cancel blocks pending sends/reminders. A send already accepted by the provider cannot be recalled; status records the delivered/in-flight part. Editing a maintenance window cancels/replaces pending jobs only after a fresh preview/confirmation.

## Routes (PLANNED)

| Method | Route | Gate |
|---|---|---|
| POST/GET | `/api/admin/broadcasts` | SuperAdmin draft/list, no send on draft |
| POST | `/api/admin/broadcasts/{id}/confirm` | SuperAdmin human, matching draft revision and audience hash |
| POST | `/api/admin/broadcasts/{id}/cancel` | SuperAdmin human |
| POST/GET | `/api/workspaces/{id}/notices` | Owner/Admin human; only workspace `role_check` audience |
| POST | `/api/workspaces/{id}/notices/{noticeId}/confirm` or `/cancel` | Owner/Admin human with draft ownership/authority |

Every route is AI OBO 403, including for a SuperAdmin OBO principal. The web submits authorized human actions; the AI only proposes. Internal incident events use the bound outbox/inbox contract, not a new publicly callable `/alerts/event` bypass.

## Separation of Concerns

Backend resolves audiences, stores confirmation and dispatches. AI drafts and summarizes. Web renders preview/confirm/cancel. Brevo handles email transport; Trigger is a compute/delay adapter and never owns approval state.

## Docker & Deploy

No new container. Reuse email sender `admin` for operator notices, existing verified sender profiles for workspace notices and existing durable jobs. Migrate reviewed draft/audience state through the backend release step. Operator confirms the fixed SuperAdmin incident audience before enabling automatic alerts.

## Acceptance Criteria

- [ ] Threshold breach creates one deduplicated alert with both channel intents; deterministic fallback works without the model.
- [ ] A draft sends nothing; confirm requires the same revision, audience, time and authorized human.
- [ ] Role-check reminder previews N workspace members; Admin cannot select another workspace or all platform users.
- [ ] Replayed confirm/retry never double-sends; partial provider failure retries only the failed channel.
- [ ] Cancellation or revoked authority prevents remaining sends; already delivered messages remain honestly recorded.
- [ ] Reminder for 14:00 EAT fires at 13:00 EAT when requested one hour before; timezone changes require preview.
- [ ] No AI OBO caller can draft/confirm/send through human notice routes; backend-bound summary callbacks cannot select recipients.

## Verification

`dotnet build`, SQL-enabled `dotnet test`, mocked Brevo/Trigger clock/failure/replay tests, Postman authorization matrix, contract-sync. No real email is sent by CI.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
