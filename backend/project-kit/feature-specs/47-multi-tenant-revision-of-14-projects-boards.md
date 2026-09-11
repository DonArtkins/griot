# Feature 47 — Multi-Tenant Revision of Feature 14 (Projects, Boards & Columns) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 14; the original spec 14 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

Projects/boards/columns carry the tenant stamp (`OrganizationId`, boards/columns storing their project's org id), visibility rules become org-role-aware — **Admin sees ALL projects in own org** (`org.projects.view_all`), **PM manages assigned projects only**, **Client sees attached projects' progress view only** — and `ProjectStatus` gains `Handoff`/`Maintenance`/`PostDeploymentSupport`.

## Dependencies

- Spec 37 (columns/filters), spec 29 (`OrganizationMembers`), spec 34 (`ProjectClients`)
- Implemented spec 14 (project/board/column CRUD, membership scoping ✅)
- Specs 30/31/39/40 (claims, permission gates, REST/GraphQL org scoping)

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §2, §3, §7
- `diagrams/erd/multi-tenant-amendment.md` (tenant columns + `ProjectStatus` gains)
- Original spec: `backend/project-kit/feature-specs/14-projects-boards-columns.md`

## Agent Skills To Use

- `backend/.agents/skills/dotnet-ef-core/SKILL.md`
- `backend/.agents/skills/hotchocolate-graphql/SKILL.md` (org-scoped GraphQL project queries)
- Root `.agents/skills/contract-sync/SKILL.md`

## Files Owned

- `Griot.Application/Services/ProjectService.cs` (org stamp + visibility resolution), board/column services
- `Griot.Api/Controllers/ProjectController.cs` (+ board/column controllers) — visibility gates
- `Griot.Domain/Enums/ProjectStatus.cs` (append-only enum gains)
- Visibility + cross-tenant test suite (xUnit; Postman via spec 43 revision)

## Implementation Notes

- `Projects`, `Boards`, `Columns` all carry NOT NULL `OrganizationId`; boards/columns store their **project's** org id (denormalized, spec 37) so global filters compose without joins; backfill resolves the project ownership chain.
- Visibility resolution (server-side, per request): **Admin** (`OrganizationRole.Admin`/`Owner`, `org.projects.view_all`) → **all projects in own org only** — the org boundary is absolute, `view_all` never crosses companies; **PM** (`project.manage`) → **assigned projects only** (assignment resolved from the org's project-assignment records — spec 48's task assignment is not the same set); **Member** → projects of workspaces they belong to; **Client** → `ProjectClients`-attached projects, progress view only (spec 34/40 revisions — no board internals).
- Project/board/column create/update/delete carry `[RequirePermission("project.manage")]` (+ `org.settings.manage` where org-wide settings change); list endpoints follow the spec-18 contract (`page`/`pageSize`/`sortBy` whitelist `CreatedAt`/`Name`, `archived` filter, `q` over `Name`).
- Cross-org project/board/column id → **404** via global filters (REST) / empty (GraphQL) — no existence disclosure.
- `ProjectStatus` gains `Handoff`, `Maintenance`, `PostDeploymentSupport` — **append-only** ordinals; transitions into/out of these states are owned by spec 35 (handoff flow), this revision only extends the enum + status filter/whitelist entries.
- Boards/columns keep their position/swap conventions from implemented spec 14; org stamp never changes ordering semantics.
- GraphQL project/board/column queries run under the tenant request context (spec 40 revision) — no resolver-level org `Where`; DataLoader batching stays org-scoped.
- Suspended org → project/board/column **writes** 403 `org_suspended`; reads remain available (client progress included).
- Project DTOs gain `organizationId`; archived-filter and activity-feed project queries inherit org scoping automatically via filters.

## Separation of Concerns

Visibility rules resolved in `Griot.Application` (policy) on top of EF isolation (`Griot.Infrastructure`); GraphQL/REST stay presentation-thin (specs 39/40 revisions). Handoff/maintenance **transitions** are spec 35; enum gains here are the data contract only.

## Acceptance Criteria

- [ ] Projects/boards/columns stamped from the project's org; backfill + create-time stamping verified (SQL-gated)
- [ ] Admin sees every project in own org and **zero** in another; PM sees assigned projects only (two-org seeded matrix test)
- [ ] Client sees attached projects through the progress view only — no board internals (field-allowlist test)
- [ ] `ProjectStatus` accepts `Handoff`/`Maintenance`/`PostDeploymentSupport`; existing ordinals unchanged
- [ ] Cross-org project/board/column id → 404/empty; suspended-org writes → 403 `org_suspended`
- [ ] `dotnet build` + `dotnet test` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.