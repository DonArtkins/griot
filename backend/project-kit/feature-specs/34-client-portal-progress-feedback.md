# Feature 34 — Client Portal: Progress Views, Feedback & AI Client Boundary (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented**

## What This Delivers

**Client onboarding onto projects + the client support surface** (the system's most important product axis — client satisfaction): attach a client to a project (`ProjectClients`, `Client` org role), give them a server-enforced **different view** of the project (percent-complete, milestones, activity digest — no internal task/board internals), let them **comment and suggest edits** (`ClientFeedback` → routed to the assigned ProjectManager), and extend the AI capability gateway so client-facing AI answers **only** from client-scoped data (backend 25 bump, ai 13).

## Dependencies

- Specs 29 (schema), 30 (JWT `role=client`), 31 (permission keys `client.*`) · 22 (fan-out to PM) · 25 (AI gateway bump).

## Files Owned

- `Griot.Application/Services/ClientPortalService.cs`, `ClientFeedbackService.cs`
- `Griot.Api/Controllers/ClientPortalController.cs` + `ClientFeedbackController.cs`, GraphQL client-view types

## Implementation Notes

- `POST /api/projects/{id}/clients {email, displayName, accessLevel}` (PM/Admin with `client.manage`) → scoped `OrganizationInvites` → on accept, user gets `OrganizationRole.Client` + `ProjectClients` row. `DELETE …/clients/{userId}` sets `RemovedAt` (soft — history kept).
- Client read endpoints (`GET /api/client/projects`, `GET /api/client/projects/{id}/progress`): computed progress (task counts by `TaskStatus`, percent-complete, milestone timeline from boards/columns, sanitized activity digest). Response shape is a dedicated `ClientProjectViewDto` — **never** the internal board/task DTOs.
- Feedback: `POST /api/client/projects/{id}/feedback {body, kind}` (client) → `ClientFeedback` row + notification to the assigned PM (spec 22) + audit event; PM routes: `GET /api/projects/{id}/feedback`, `POST …/feedback/{fid}/respond {status, pmResponse}`. Clients see only their own feedback + public PM responses.
- AI boundary (extends spec 25): client OBO context (`role=client`) exposes only `client.read_progress` / `client.add_feedback` capabilities; ai 13's golden transcripts assert no internal task data ever reaches a client surface.

## Acceptance Criteria

- [ ] Client invite → accept → sees only attached projects' progress view
- [ ] Progress view exposes no internal board internals (contract test on DTO shape)
- [ ] Client feedback reaches the PM as a notification; PM can resolve/reject with a response the client can read
- [ ] Client OBO AI calls are capability-restricted; cross-project data returns empty
- [ ] `dotnet build` + `dotnet test` green
