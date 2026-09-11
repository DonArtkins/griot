# Feature 33 — Company Offboarding: Export, Retention, Purge (SuperAdmin) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented**

## What This Delivers

Clean, complete, **provable** tenant exit (research §8): `POST /api/organizations/{id}/offboard` → data export bundle (JSON of every org row; artifacts via blob spec 11) → `Status = Offboarding` + retention window (`Organizations:RetentionDays`, default 30) → purge/anonymize job → `Status = Archived` + `Purged` lifecycle event. Client-facing handoff of *individual projects* is spec 35 (different flow — data is retained there).

## Dependencies

- Specs 29/32 · 11 (blob, for artifacts) · 20 (audit) · 22 (final notification to company owner).

## Files Owned

- `Griot.Application/Services/OrganizationOffboardingService.cs`, export DTOs, purge job (`IHostedService`)

## Implementation Notes

- Export is a structured JSON bundle per entity family (org, members, workspaces→projects→boards→columns→tasks→comments, attachments metadata, feedback, handoffs) with manifest + checksum; delivered via Brevo link (blob key) to the company owner + SuperAdmin.
- Retention window: logins still work read-only during `Offboarding`; all writes 403. Purge: delete/anonymize personal data (`Users` rows that have no other active membership are anonymized, never hard-deleted while refresh families reference them — tombstone pattern per research §8.2/§8.3), remove org rows app-managed (all six org FKs are ON DELETE NO ACTION — Restrict): leaves-first delete order — workspace/project subtrees (boards → columns → tasks → comments/attachments), then `OrganizationMembers`, `Roles`, `OrganizationInvites`, `OrganizationLifecycleEvents` — before deleting the `Organizations` row; each org-scoped chunk runs in its own transaction and records a resumable `OrganizationLifecycleEvents` progress checkpoint (spec-41 pattern); keep required legal/financial records.
- Purge is a background job: idempotent, resumable, cap-aware (bulk ops spec 06), emits progress lifecycle events (`OrganizationLifecycleEvents` checkpoint per org-scoped chunk; re-run resumes from the last checkpoint — spec-41 pattern). Kill-switch: cancel within the retention window → `Reactivated`.

## Acceptance Criteria

- [ ] Export bundle downloads and validates against the manifest; owner + SuperAdmin notified
- [ ] Retention window honored; cancel/reactivate works before purge
- [ ] After purge: zero org rows remain; anonymized user tombstones survive FK integrity; audit trail retained
- [ ] Every step audited (`OffboardStarted` → `DataExported` → `Offboarded` → `Purged`)
- [ ] `dotnet build` + `dotnet test` green
