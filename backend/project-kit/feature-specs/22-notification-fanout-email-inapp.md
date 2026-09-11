# Backend Feature Spec 22 — Notification Fan-Out: In-App + Email

**Status:** PLANNED — in-app routes exist (spec 16 ✅) but nothing *creates* notifications from product events, and no email notification path exists. The email infrastructure (spec 12 single verified sender, `IEmailService`) is implemented and reused here.

## What This Delivers

When something happens in a workspace, the affected users get an **in-app notification always** and a **transactional email** when they've enabled it — through the exact Email-only contract of spec 12 (single verified sender, best-effort, never blocking the request).

## Dependencies

- Feature 16 (✅ notification routes/read-models), Feature 12 (✅ `IEmailService`, branded templates), Feature 13 (✅ membership resolution for recipients).

## Context To Read First

- `docs/communication/COMMUNICATION-GUIDE.md` (sender rules — spec 12's single verified sender carries every notification email)
- `backend/project-kit/context/data-layer.md` (`NotificationType` enum: `Mention · Assignment · DueDate · System`)

## Fan-out matrix (planned)

All notification emails send from spec 12's **single verified sender** `Brevo:FromEmail` (no per-event sender keys — the `Brevo:Senders:<Key>` profile map was removed on 2026-09-11).

| Trigger event | `NotificationType` | Recipients |
|---|---|---|
| Task assigned to a member | `Assignment` | assignee (+ actor if different, excluded by default) |
| `@mention` in a comment | `Mention` | mentioned user(s) |
| Due-date reminder window (24h; scheduled agent post spec 09 writes via API — the *backend* fan-out endpoint is what the agent calls) | `DueDate` | assignee |
| Invite sent / accepted | `System` | inviter / invitee |
| Role change / removal | `System` | affected member |
| Workspace-level incidents surfaced by spec 20 (optional flag, off by default) | `System` | Owner/Admin |

- Every event: 1 `Notifications` row per recipient (in-app, the source of truth) → then email fan-out **only** for recipients whose preference allows it.
- Email body: branded template (spec 12 shell), deep link `VITE_API_URL`-agnostic `{webBase}/board/{boardId}?task={taskId}`; no per-profile reply-to (optional per-call `ReplyTo`, default none); `X-Griot-Notification-Id` custom header for tracing.

## New schema (the one migration in this spec)

`NotificationPreferences` (1:1 with `Users`): `UserId` PK/FK cascade, `EmailEnabled` bit default **true**, `InAppEnabled` bit default true, `MutedNotificationTypes` nvarchar(max) JSON array, `UpdatedAt`. Migration `AddNotificationPreferences` (EF, spec-02 conventions). No changes to existing tables.

## New routes (planned)

| Route | Purpose | Gate |
|---|---|---|
| `GET /api/notifications/preferences` | current user's prefs (creates defaults lazily) | authenticated |
| `PUT /api/notifications/preferences` | update prefs (self only) | authenticated |
| `POST /api/notifications/fanout` *(internal)* | `GRIOT_SERVICE_TOKEN`-gated event → fan-out used by scheduled agents (ai/) post spec 09; JWT callers rejected | service token only |

Fan-out service lives in `Griot.Application` (`INotificationFanoutService`); email sending via existing `IEmailService`; recipients resolved through `WorkspaceMembers`.

## Failure isolation (spec 12 semantics)

Email is **best-effort**: a Brevo outage must not fail the triggering API request (task assignment still succeeds, 200/201, notification row exists). Delivery failure → `ErrorLogs` row (spec 20) + warning log. The one surfaced-failure exception in the system remains `/api/auth/otp/request` (502) — this spec adds **no** new surfaced-failure path.

## Rate limiting

Fan-out emails inherit the global limiter + spec 12 Redis gates; `fanout` endpoint additionally capped at 30/min per principal (spec 19 partition). Brevo's 300/day budget stays protected: per-event fan-out is capped at 25 recipients (larger audiences → digest, post-bootcamp).

## Separation of Concerns

Event detection + recipient resolution: `Griot.Application`. SMTP/API call + templates: `Griot.Infrastructure.Email` (existing). Routes: thin controllers. Web/mobile only ever read `Notifications` — they never compute fan-out.

## Docker & Deploy

One new env var `Web__BaseUrl` (email deep links) — added to `.env.example` + integration-contracts in the same branch. Compose unchanged otherwise.

## Out of Scope

Push notifications (mobile/web FCM), digest scheduling (ai/ spec 03 calls the fanout endpoint instead), SMS/WhatsApp (removed, spec 12).

## Acceptance Criteria (all pending)

- [ ] Assigning a task creates a `Notifications` row for the assignee; `GET /api/notifications` + unread-count reflect it
- [ ] `@mention` in a comment creates a `Mention` notification for the mentioned user
- [ ] With `EmailEnabled=true`, the email send is attempted via `IEmailService` from the single verified sender; with `false`, no email call (unit-tested with the existing Brevo test double)
- [ ] Brevo failure during fan-out → request still succeeds; `ErrorLogs` row exists
- [ ] `PUT /api/notifications/preferences` persists; `MutedNotificationTypes` suppresses both channels for that type
- [ ] `POST /api/notifications/fanout` without service token → 401; with it → 202
- [ ] Postman folder 10 + 14 updated (preferences GET/PUT + fanout negative test)
- [ ] Migration `AddNotificationPreferences` applies clean on a fresh + an existing DB

## Multi-Tenant Update (2026-09-11 — PLANNED)

- **Fan-out becomes org-scoped:** every `Notifications` row and every email recipient is resolved **within `ITenantContext`** (active `OrganizationMembers` only); the service-token `fanout` endpoint (scheduled agents, spec 09/44) requires the delegation's `OrganizationId` and never crosses it.
- **New recipients (PLANNED):** the **company owner/Admin** for onboard/suspend/reactivate/offboard lifecycle events (specs 32/33/45), the **assigned ProjectManager** for client feedback posted (spec 34), the **client** for handoff `AwaitingClientAcceptance` and PM responses (specs 34/35).
- `NotificationType` gains tenant/client values (org lifecycle, client feedback, handoff) appended to the enum — `data-layer.md` + `docs/api` contract-synced in the same branch.
- `NotificationPreferences` semantics unchanged (per-user, org-independent); suspend/offboard lifecycle notices honor `MutedNotificationTypes` like every other fan-out.
- The 25-recipient per-event cap is retained; org **broadcasts** never route through raw fan-out — they use spec 27's draft/confirm path (its Multi-Tenant Update separates the platform channel from org-scoped confirmed notices).
- Cross-org mention/assignment attempts resolve zero recipients and write an `ErrorLogs`/audit row (org id on the row — spec 51 revision) instead of notifying foreign users.
- Client fan-out visibility: clients receive only client-surface notifications (progress/handoff/PM-response) — never internal board chatter (field/type allowlist test in spec 49 revision).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.