# Feature 50 — Multi-Tenant Revision of Feature 17 (Attachments Metadata & Webhooks) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 17; the original spec 17 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

The `Attachments` metadata surface org-stamped (`Attachments.OrganizationId` — blob storage itself stays **pending spec 11**), and webhook routes **org-tagged**: the Trigger.dev HMAC callback inbox (spec 09/20 contract) carries `OrganizationId` for every org-bound job so callback validation is tenant-checked.

## Dependencies

- Spec 37 (columns/filters), spec 11 (blob — its Multi-Tenant Update owns the `org/{orgId}/…` key prefix; storage itself stays its dependency)
- Implemented spec 17 (attachment metadata CRUD, validation, activity logging ✅)
- Specs 09/20 (HMAC webhook contract + durable inbox), spec 48 (task org stamp)

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §1, §7 (handoff documents preview — spec 35)
- Original spec: `backend/project-kit/feature-specs/17-attachments-metadata.md`
- `docs/api/ai-service-token-contract.md` (callback HMAC semantics)

## Agent Skills To Use

- `backend/.agents/skills/dotnet-ef-core/SKILL.md`
- Root `.agents/skills/contract-sync/SKILL.md`

## Files Owned

- `Griot.Application/Services/AttachmentService.cs` (org stamp + task-org binding)
- `Griot.Api/Controllers/AttachmentController.cs` (gates), webhook inbox org-stamp changes
- Attachment/webhook org tests (xUnit; Postman via spec 43 revision)

## Implementation Notes

- `Attachments` gains NOT NULL `OrganizationId`, backfilled from the attachment's task → board → project chain (spec 37); create-time stamping from `ITenantContext`, task binding validated within the active org (foreign task → 404 all-or-nothing per spec 41 revision).
- Attachment metadata validation conventions from implemented spec 17 unchanged (size/MIME/extension rules, activity logs); the 100 MB workspace quota stays as-is, with a **per-org quota** evaluated as PLANNED once spec 11 ships (no billing this wave — quota display metadata only).
- **Blob pending spec 11:** `Attachments.BlobKey`/`StorageUrl` behavior is spec 11's revision (`org/{orgId}/…` key prefix, per-org export bundles, handoff documents) — this revision adds only the metadata tenant stamp and keeps the implemented v1 storage contract unbroken until 11 lands.
- Upload gated by **`task.manage`**, delete by the same + existing ownership rules; the surface is org-internal — a client-role principal never lists/downloads internal task attachments (client-visible documents travel the `HandoffDocuments` path, spec 35).
- **Webhook routes org-tagged:** the Trigger.dev HMAC callback inbox rows (spec 20's planned durable dispatch) carry `OrganizationId` bound to the job; a callback is valid only when its job's org matches the inbox row's org — replay/expiry rules (401 bad HMAC, 413 oversized, 503 unconfigured, freshness/idempotency) unchanged from implemented 09.
- Outbound webhook/event records created by backend paths (Trigger.dev enqueue via the spec-20 outbox) carry the org id so the compute side stays tenant-correlated without ever owning data.
- Activity/audit rows for attachment effects inherit the org stamp automatically (spec 20 pipeline) — writers unchanged beyond the stamp.
- Suspended org: attachment upload/delete → 403 `org_suspended`; reads remain.

## Separation of Concerns

Blob transport/quota against the real store is spec 11; handoff document flow is spec 35; webhook **dispatch durability** is spec 20. This revision owns the metadata tenant stamp, the task-org binding and the org-tagged inbox contract only.

## Acceptance Criteria

- [ ] Attachments backfilled + stamped from the task's org; attachment on a foreign-org task → 404
- [ ] Upload without `task.manage` → 403; suspended-org upload/delete → 403 `org_suspended`
- [ ] HMAC inbox rows carry `OrganizationId`; org-mismatched callback rejected without disclosure
- [ ] Client-role principal cannot list/download internal attachments (surface separation test)
- [ ] Implemented spec-17 validation rules (size/MIME/extension) still green
- [ ] `dotnet build` + `dotnet test` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.