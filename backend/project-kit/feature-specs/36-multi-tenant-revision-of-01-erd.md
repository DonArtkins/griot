# Feature 36 — Multi-Tenant Revision of Feature 01 (ERD & Schema Design) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 01; the original spec 01 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

The **ERD amendment v2** process for the multi-tenant wave: the entity/enum/relationship changes in `diagrams/erd/multi-tenant-amendment.md` must be transcribed into and **approved in Figma Make** (hard rule 4 — ERD before schema) before any `Organizations`-related migration code exists. The amendment defines **9 new tables** (`Organizations`, `OrganizationMembers`, `Roles`, `OrganizationInvites`, `ProjectClients`, `ClientFeedback`, `ProjectHandoffs`, `HandoffDocuments`, `OrganizationLifecycleEvents`), the **changed-entity set** (tenant `OrganizationId` on `Workspaces, Projects, Boards, Columns, TaskItems, Comments, Attachments, Invites, Notifications`; nullable `OrganizationId` on `ApiLogs, ErrorLogs, AuditLogs, ActivityLogs`; `Users.PlatformRole`), **new enums** and **new relationships** — all PLANNED until the diagram export is registered in `diagrams/erd/`.

## Dependencies

- Spec 01 (implemented ERD v1.0.0 — unchanged entities stay authoritative there)
- `diagrams/erd/auth-family-amendment.md` (the precedent: amend, do not re-open the full v1.0.0 ERD)
- Spec 29 (foundation spec that consumes this diagram)

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §2 (canonical field list)
- `diagrams/erd/multi-tenant-amendment.md` (the amendment being approved)
- `diagrams/erd/griot-erd-v1.0.0.png` (baseline the amendment modifies)
- Original spec: `backend/project-kit/feature-specs/01-erd-and-schema-design.md`

## Agent Skills To Use

- Root `.agents/skills/figma-make-erd/SKILL.md` (transcription + export process)
- Root `.agents/skills/contract-sync/SKILL.md` (entity names are contract once approved)

## Files Owned

- `diagrams/erd/multi-tenant-amendment.md` (kept in sync with the Figma Make source)
- Figma Make ERD amendment v2 (export registered in `diagrams/erd/`)
- NO schema code — migrations belong to spec 37; this spec is design-gate only

## Implementation Notes

- Amendment, not rewrite: `griot-erd-v1.0.0` remains authoritative for all unchanged entities; v2 only adds/changes what the amendment itemizes.
- 9 new tables exactly as in `diagrams/erd/multi-tenant-amendment.md`, with their key columns, unique indexes (`UX_Organizations_Slug`, `UX_OrganizationMembers_Org_User`, `UX_ProjectClients_Project_User`, `UX_ProjectHandoffs_ProjectId`) and FK cascade rules.
- New enums: `OrganizationStatus` (Active/Suspended/Offboarding/Archived), `OrganizationPlan` (Free/Pro/Enterprise — display metadata only, no payments this wave), `OrganizationRole` (Owner/Admin/ProjectManager/Member/Client/Custom), `PlatformRole`, `ClientFeedbackKind` (Comment/Suggestion/EditRequest), `ClientFeedbackStatus` (New/Acknowledged/Resolved/Rejected), `HandoffStatus` (NotStarted/InProgress/AwaitingClientAcceptance/Completed), `HandoffDocumentKind` (Manual/Credential/Design/Report/Other), `LifecycleEventKind` (Onboarded/Suspended/Reactivated/OffboardStarted/DataExported/Offboarded/Purged).
- `ProjectStatus` gains `Handoff`, `Maintenance`, `PostDeploymentSupport` — **append-only**; existing ordinals must not move (existing DB rows stay valid).
- New relationships with cardinality per the amendment: `Organizations 1—N {OrganizationMembers, Roles, OrganizationInvites, OrganizationLifecycleEvents, Workspaces}`; `Users 1—N {OrganizationMembers, ProjectClients}`; `Projects 1—N {ProjectClients, ClientFeedback}`, `1—1 ProjectHandoffs`; `ProjectHandoffs 1—N HandoffDocuments`.
- The isolation contract (Pool model: `ITenantContext` → EF global query filters → repository guard — SQL Server has no RLS) is annotated on the diagram so implementers of specs 37/39/40 read it from the same source.
- Diagram must show which entities are **NOT** tenant-stamped (e.g. `RefreshTokens`, `OtpChallenges`, `Users` org-free except `PlatformRole`) to prevent over-scoping.
- Approval evidence = the exported diagram registered in `diagrams/erd/` + a contract-sync pass updating `backend/project-kit/context/data-layer.md` entity list in the same branch.

## Separation of Concerns

This spec owns the **design gate only**. Schema code is spec 37; REST/GraphQL behavior is specs 39/40; lifecycle semantics are specs 32/33; client/handoff surfaces are specs 34/35. No migration file may be created under this spec.

## Acceptance Criteria

- [ ] ERD amendment v2 transcribed into Figma Make and **approved**, export registered in `diagrams/erd/` before any migration merges
- [ ] Diagram contains all 9 new tables, the changed-entity set, all 9 new enums and the `ProjectStatus` additions (append-only)
- [ ] Cardinality of every new relationship matches `diagrams/erd/multi-tenant-amendment.md`
- [ ] Isolation-contract annotation (ITenantContext → global filters → repository guard) visible on the approved diagram
- [ ] Repo grep proves no `Organizations`-related migration/schema code existed before the approval landed (rule 4 gate)

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.