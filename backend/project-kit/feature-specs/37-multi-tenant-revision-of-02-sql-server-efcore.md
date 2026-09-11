# Feature 37 — Multi-Tenant Revision of Feature 02 (SQL Server + EF Core Implementation) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 02; the original spec 02 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

The tenant schema on the implemented EF Core layer: migration **`AddMultiTenantColumns`** adds `OrganizationId` to every tenant-owned table, **backfills existing rows from the ownership chain**, applies `IX_{table}_OrganizationId` indexes, adds the 9 new tenant tables and `Users.PlatformRole`, and switches on **EF Core global query filters** on every tenant entity so a forgotten `Where` can never cross tenants (Pool model — SQL Server has no RLS).

## Dependencies

- Spec 36 (approved ERD amendment v2 — hard gate, no migration before it)
- Implemented spec 02 (EF Core conventions, `IGenericRepository<T>`, migration patterns)
- Spec 29 (tenant middleware/context consumption points)

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §1 (Pool discipline, three-layer boundary)
- `diagrams/erd/multi-tenant-amendment.md` (columns/indexes/FKs)
- Original spec: `backend/project-kit/feature-specs/02-sql-server-efcore-implementation.md`

## Agent Skills To Use

- `backend/.agents/skills/dotnet-ef-core/SKILL.md`
- `backend/.agents/skills/sql-server-2022/SKILL.md`
- Root `.agents/skills/contract-sync/SKILL.md`

## Files Owned

- `Griot.Infrastructure/Migrations/AddMultiTenantColumns` (+ model builder changes)
- `Griot.Domain/Organizations/*` (entities + all new enums)
- `Griot.Infrastructure/Tenancy/TenantQueryFilterExtensions.cs` (global filter wiring), save-path tenant guard in `IGenericRepository`
- Updated `backend/project-kit/context/data-layer.md`

## Implementation Notes

- Migration `AddMultiTenantColumns`: NOT NULL `OrganizationId` on `Workspaces, Projects, Boards, Columns, TaskItems, Comments, Attachments, Invites, Notifications`; **nullable** on `ApiLogs, ErrorLogs, AuditLogs, ActivityLogs`; `Users.PlatformRole` (default `User`); creates the 9 new tables per the approved amendment.
- Backfill: existing single-tenant rows are assigned to one **bootstrap default organization** created inside the migration (owned by the SuperAdmin identity from spec 30's bootstrap) — the ownership chain (workspace → project → board/column → task/comment/attachment/invite/notification) is resolved transitively so every child row carries its workspace's org id; backfill is idempotent and re-runnable.
- `Boards` and `Columns` are denormalized with their **project's** org id (never independently resolved) so global filters compose without joins.
- EF Core global query filters: `HasQueryFilter(e => e.OrganizationId == tenantContext.OrganizationId)` on every tenant entity; platform paths (SuperAdmin, outbox workers, migrations) use an explicit `WithTenantScope(null)` bypass that is **audit-logged**; `IgnoreQueryFilters()` anywhere else fails review.
- Write assert: the save path throws `DomainError(Forbidden, "cross-tenant")` when a tracked entity's `OrganizationId` ≠ the active tenant context — the third isolation layer in the guide.
- Indexes: `IX_{table}_OrganizationId` on every tenant table; unique `UX_Organizations_Slug`; unique `UX_OrganizationMembers_Org_User`; `IX_OrganizationMembers_UserId`; `IX_{logTable}_OrganizationId` (filtered where NULL-heavy, per SQL Server 2022 conventions).
- FK cascade rules exactly per the amendment (`Organizations → OrganizationMembers/Roles/OrganizationInvites/OrganizationLifecycleEvents` cascade; `Workspaces` cascade under org per ERD; member/invite/user FKs per v1 conventions).
- New enums stored per spec-02 conventions; `ProjectStatus` gains `Handoff, Maintenance, PostDeploymentSupport` **append-only** (existing ordinal values unchanged).
- Seeder/update: system `Roles` rows are seeded by the onboarding flow (spec 32), never by `HasData` (they are per-org rows, not global).
- Contract-sync: `data-layer.md`, `api-surface.md` conventions and `docs/database/DATABASE-DESIGN.md` index section updated in the same branch.

## Separation of Concerns

Entities/enums + filters + guard: `Griot.Domain` / `Griot.Infrastructure`. Tenant resolution (JWT `org` → `ITenantContext`) lives in spec 39's middleware, not in EF. Migrations never contain business logic.

## Acceptance Criteria

- [ ] Migration applies clean on a fresh DB **and** on an existing populated DB (backfill idempotent; second run no-ops)
- [ ] Every tenant entity has a global query filter; cross-tenant read returns **empty** (SQL-gated integration test)
- [ ] Cross-tenant write (mismatched `OrganizationId`) throws at the save path (unit + integration test)
- [ ] All amendment indexes exist and are verified SQL-gated; `UX_Organizations_Slug` + `UX_OrganizationMembers_Org_User` unique enforced
- [ ] `ProjectStatus` new values append-only; pre-existing rows unchanged after migration
- [ ] `dotnet build` + `dotnet test` green; compose stack healthy

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.