# Feature 29 — Multi-Tenant Foundation: Organizations & Tenant Isolation (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **IMPLEMENTED — 2026-09-11**

**Implementation record:** commit `9447e15` on this feature's branch (pushed after
explicit user approval): fail-closed `ITenantContext`/`TenantGuard` scoping (403 without
org scope; GraphQL writes inherit the tenant and cross-tenant chains resolve as not-found),
`OrganizationId` on all tenant tables + observability stamps, `CreateWorkspace` org-stamped
with the POST owner-member user-data fix, DataLoaders copy the tenant scope onto pooled
contexts, `DevObservabilitySeeder` migration-race resilience, migration
`20260911190926`
(17 tenant-filtered entities, 30 org indexes, idempotent ownership-chain backfill +
quarantine org) and ERD Amendment v2. Verified: `dotnet build` 0W/0E; `dotnet test` 152
passed / 8 SQL-skipped / 0 failed. **Outstanding before COMPLETE:** tenancy ERD approval
(`diagrams/erd/multi-tenant-amendment.md`) only — the migration-apply gate is CLOSED
(2026-09-11: the cascade variant failed with SQL 1785, was regenerated as
`20260911190926` with org FKs ON DELETE NO ACTION, the database was rebuilt from all
migrations, and the user's `dotnet ef database update` now reports the database is
already up to date).
See the [preflight findings and completion plan](../../../docs/planning/BACKEND-29-PREFLIGHT-2026-09-11.md)
for the pre-implementation snapshot.

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

- [ ] ERD amendment approved in Figma Make and exported before the migration merges — amendment source `diagrams/erd/multi-tenant-amendment.md` (v2) committed; **Figma approval still outstanding**
- [x] Every tenant table carries `OrganizationId` + index; backfill migration is idempotent — migration `20260911190926` (30 org indexes; ownership-chain backfill, unmatched rows → quarantine org, re-runnable SQL). Org FKs are **ON DELETE NO ACTION** (Restrict): cascading from `Organizations` to both `Workspaces` and `Projects` = multiple cascade paths (SQL error 1785), so org removal is app-managed via the spec-33 offboarding purge; the failed cascade variant `20260911151030` was regenerated and the database reset.
- [x] Cross-tenant read returns empty and cross-tenant write throws (integration test, SQL-gated) — `TenantIsolationTests` (fail-closed 403 without org scope; org-mismatched chains resolve as not-found); SQL-gated cases among the 8 skipped without a server
- [x] `POST /api/workspaces` returns member user data (`displayName`/`email`) identical to `GET /api/workspaces` (fixes the reported Postman defect) — `CreateWorkspaceAsync` hydrates the owner `WorkspaceMember.User`
- [x] `dotnet build` + `dotnet test` green; compose stack healthy — build 0W/0E; 152 passed / 8 SQL-skipped / 0 failed; SQL Server 14333, PostgreSQL 5433, Redis 6380 verified up (2026-09-11)
