# Feature 35 — Project Handoff, Client Offboarding & Maintenance Phase (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented**

## What This Delivers

Project close-out: a **handoff flow** (checklist → deliverables/credentials/design docs → **AI-generated client user manual** stored in `HandoffDocuments`) → client acceptance → **client offboarding** (access narrowed, relationship preserved) → the project enters `Maintenance` / `PostDeploymentSupport` where the client keeps raising support requests and the PM (or AI triage, ai 14) responds. **Client + project data are retained** after client offboarding so maintenance stays possible (full purging only happens at company offboarding, spec 33).

## Dependencies

- Specs 29 (schema), 34 (client portal), 11 (blob for documents), 24 (report/export surface), ai 14 (manual generator).

## Files Owned

- `Griot.Application/Services/ProjectHandoffService.cs`, `MaintenanceService.cs`
- `Griot.Api/Controllers/ProjectHandoffController.cs`, `MaintenanceController.cs`

## Implementation Notes

- `POST /api/projects/{id}/handoff` (PM) → `ProjectHandoffs(InProgress)` + default checklist JSON (deliverables, credentials, environments, documentation, training, warranty/support terms); checklist item transitions audited.
- Documents: `POST …/handoff/documents` (upload → blob key, backend 11) and `POST …/handoff/generate-manual` (queues the ai 14 agent through the spec 20 outbox → writes `HandoffDocuments(Kind=Manual, GeneratedByAi=true)`). Credential-kind documents are permission-gated (`client.manage` + `AwaitingClientAcceptance` only).
- `POST …/handoff/submit` → `AwaitingClientAcceptance` (client notified via 22); `POST /api/client/projects/{id}/handoff/accept` → `Completed` + project `Status = Maintenance`; `reject {reason}` returns it to `InProgress`.
- Client offboarding: `POST /api/projects/{id}/clients/{userId}/offboard` → `RemovedAt` set, portal access narrowed to read-only + maintenance requests; `ProjectStatus` may move to `PostDeploymentSupport`.
- Maintenance: `POST /api/client/projects/{id}/maintenance-requests` (client; creates `ClientFeedback(Kind=Suggestion)` + routes to PM; SLA metadata field optional), PM/AI respond; all events audited; reports surface via spec 28/24.

## Acceptance Criteria

- [ ] Handoff checklist → documents (incl. AI manual) → client accept end-to-end; project lands in `Maintenance`
- [ ] Offboarded client retains read-only + maintenance-request access; nothing else
- [ ] AI manual generation survives retry (outbox, spec 20); duplicate generations idempotent
- [ ] Client/project data retained post-offboarding (no purge)
- [ ] `dotnet build` + `dotnet test` green
