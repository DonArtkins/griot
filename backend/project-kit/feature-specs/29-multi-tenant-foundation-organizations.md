# Feature 29 — Multi-Tenant Foundation: Organizations & Tenant Isolation (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented**

**2026-09-11 checkpoint:** partial, uncommitted implementation exists on this
feature's branch; it is not accepted implementation. ERD approval, schema alignment,
complete tenant enforcement and SQL acceptance remain outstanding. See the
[preflight findings and completion plan](../../../docs/planning/BACKEND-29-PREFLIGHT-2026-09-11.md).
Build passed and 152 tests passed with 8 SQL tests skipped; acceptance boxes below
remain unchecked. No migration or commit/push was performed during this preflight.

## What This Delivers

The tenant root of the platform: `Organizations` (Companies) become the unit of isolation and lifecycle. Every tenant-owned table gains `OrganizationId`, EF Core global query filters + a request-level `ITenantContext` enforce isolation (Pool model — `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §1), and the ERD amendment (`diagrams/erd/multi-tenant-amendment.md`) is approved in Figma Make before any migration code exists (hard rule 4).

## Dependencies

- Approved ERD amendment (Figma Make, rule 4). Spec 20 ✅ (audit writers stamp `OrganizationId` from day one).
- Spec 02 (EF Core), 04 (REST), 05 (GraphQL) — all extended, not replaced.

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` (canonical contract)
- `diagrams/erd/multi-tenant-amendment.md` (planned ERD amendment)
- `research/LYNCXS-MULTI-TENANT-SYSTEMS-ENGINEERING.md` §1–2 (Pool discipline)

## Agent Skills To Use

- `backend/.agents/skills/dotnet-ef-core/SKILL.md`, `sql-server-2022/SKILL.md`

## Files Owned

- `Griot.Domain/Organizations/*` (entities + enums), `Griot.Infrastructure/Migrations/AddMultiTenantColumns`
- `Griot.Api/Middleware/TenantResolutionMiddleware.cs`, `Griot.Application/Tenancy/ITenantContext.cs`
- `Griot.Infrastructure/Repositories/OrganizationRepository.cs` + REST/GraphQL surface for orgs (read-side only — creation/lifecycle is specs 32/33)

## Setup / Initialization

```bash
dotnet ef migrations add AddMultiTenantColumns --project src/Griot.Infrastructure
# env: none new; SUPERADMIN__EMAIL arrives with spec 30
```

## Implementation Notes

- `OrganizationId` NOT NULL on `Workspaces, Projects, Boards, Columns, TaskItems, Comments, Attachments, Invites, Notifications`; nullable on the four observability tables. Migration backfills from the ownership chain.
- Global query filters on every tenant entity; `ITenantContext` from the JWT `org` claim; writes assert the column matches or throw `DomainError(Forbidden, "cross-tenant")`. Platform-level paths (SuperAdmin, outbox workers) use an explicit `WithTenantScope(null)` bypass that is audit-logged.
- Slug uniqueness, member uniqueness (`UX_OrganizationMembers_Org_User`) enforced at the DB.
- **Includes the workspace POST fix as acceptance**: `CreateWorkspaceAsync` must reload the owner `WorkspaceMember` with `.Include(m => m.User)` (or hydrate from the user repo) before mapping, so `POST /api/workspaces` returns the member's `displayName`/`email`/`avatarUrl` exactly like GET does.

## Separation of Concerns

`ITenantContext` lives in Application; middleware in Api; filters on entities in Infrastructure. Domain has zero tenant-resolution knowledge.

## Docker & Deploy

No new container. Migration runs on deploy; compose stack re-verified.

## Out of Scope

Organization lifecycle endpoints (32/33), JWT claims (30), client portal (34).

## Acceptance Criteria

- [ ] ERD amendment approved in Figma Make and exported before the migration merges
- [ ] Every tenant table carries `OrganizationId` + index; backfill migration is idempotent
- [ ] Cross-tenant read returns empty and cross-tenant write throws (integration test, SQL-gated)
- [ ] `POST /api/workspaces` returns member user data (`displayName`/`email`) identical to `GET /api/workspaces` (fixes the reported Postman defect)
- [ ] `dotnet build` + `dotnet test` green; compose stack healthy
