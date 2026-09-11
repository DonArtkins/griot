# Feature 38 — Multi-Tenant Revision of Feature 03 (Stored Procedures & Optimized Queries) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 03; the original spec 03 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

Tenant-scoped stored procedures: every existing `usp_*` gains a mandatory **`@OrganizationId`** parameter, the pagination contract of spec 18 is carried into the updated proc signatures, new tenant procs are added for the client progress view and lifecycle events, and the observability prune proc keeps its retention logic while log rows gain `OrganizationId`.

## Dependencies

- Spec 37 (tenant columns + indexes exist)
- Implemented spec 03 (proc conventions, sqlcmd release step, `GRIOT_RUN_SQL_TESTS=1` SQL-gated tests)
- Spec 18 (pagination contract the updated procs must honor)
- Spec 25/51 revision (per-tenant log reads consume org-stamped rows)

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §1–2
- `docs/database/DATABASE-DESIGN.md` (indexes the org-scoped queries hit)
- Original spec: `backend/project-kit/feature-specs/03-stored-procedures-and-optimized-queries.md`

## Agent Skills To Use

- `backend/.agents/skills/dapper-stored-procs/SKILL.md`
- `backend/.agents/skills/sql-server-2022/SKILL.md`
- Root `.agents/skills/contract-sync/SKILL.md`

## Files Owned

- `Griot.Infrastructure/Sql/*.sql` (updated `usp_*` scripts, idempotent, re-runnable via the sqlcmd release step)
- `Griot.Infrastructure/Repositories/*` (Dapper callers pass `ITenantContext.OrganizationId`)
- SQL-gated xUnit fixtures for the new/changed procs

## Implementation Notes

- Every tenant-facing proc (`usp_GetDashboardSummary`, board/task/comment/activity listers, bulk helpers) gains `@OrganizationId UNIQUEIDENTIFIER` — **non-nullable** — and filters every table by it; the proc refuses to run without it (guard clause raising a typed error).
- Dapper callers pass `tenantContext.OrganizationId`; no proc takes org from client input. The only NULL-org path is the documented SuperAdmin platform bypass, resolved in the application layer (never in T-SQL) and audit-logged.
- `usp_GetDashboardSummary` updated per spec 18: `@Page/@PageSize/@SortBy` whitelist + `PagedResult` shape + `@OrganizationId`; Admin sees org-wide aggregates, PM sees assigned-projects aggregates (same proc, different `@Scope` input).
- New procs (PLANNED): `usp_GetClientProjectProgress` (spec 34's percent-complete/milestone/digest queries — internal board internals never leave the proc), `usp_ListOrganizationLifecycleEvents` (spec 32/33 audit feed), `usp_SearchOrganizations` (SuperAdmin platform listing, paginated per spec 18).
- `usp_PruneObservabilityLogs` retention logic **unchanged** (91/366-day windows); it simply carries the new nullable `OrganizationId` through untouched.
- All `ORDER BY` construction stays on a server-side whitelist map (column name never concatenated from input); all values parameterized — spec 18 rules re-asserted here.
- Bulk proc patterns (spec 06/41 revision) accept `@OrganizationId` and pre-validate the whole batch belongs to it before any write (all-or-nothing).
- `audit-triggers.sql` (spec 21) writes `AuditLogs` rows stamped with the affected row's `OrganizationId` — kept consistent with this spec's org-stamped audit contract.
- SQL-gated tests extend the existing `GRIOT_RUN_SQL_TESTS=1` suite: org-scoped result correctness, cross-org row invisibility at the proc level, pagination boundary checks.

## Separation of Concerns

T-SQL org filtering is the data-layer backstop behind EF global filters (spec 37) — both layers enforce the boundary independently. Application permission decisions (`[RequirePermission]`, spec 39/31) stay in `Griot.Application`/`Griot.Api`; procs never authorize, they only scope.

## Acceptance Criteria

- [ ] Every tenant proc requires `@OrganizationId` and returns zero cross-org rows (SQL-gated test with two seeded orgs)
- [ ] `usp_GetDashboardSummary` returns spec-18 `PagedResult` shape, org-scoped, Admin vs PM aggregates correct
- [ ] New tenant procs (`usp_GetClientProjectProgress`, `usp_ListOrganizationLifecycleEvents`, `usp_SearchOrganizations`) green on the SQL-gated suite
- [ ] `usp_PruneObservabilityLogs` behavior unchanged (existing 91/366-day tests still pass)
- [ ] All scripts idempotent (re-runnable via the sqlcmd release step; `IF OBJECT_ID` guards)
- [ ] `dotnet build` + SQL-gated `dotnet test` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.