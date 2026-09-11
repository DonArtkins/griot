# Feature 41 — Multi-Tenant Revision of Feature 06 (Bulk Operations & Advanced Data Handling) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 06; the original spec 06 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

Org-guarded bulk operations: every bulk path (bulk status/update/move, bulk insert, bulk delete) pre-validates the **whole batch** belongs to the active organization before any write (all-or-nothing), all bulk-write patterns stamp `OrganizationId`, and the spec-33 offboarding **purge job bulk patterns** (org-scoped, chunked, idempotent delete/anonymize) are standardized here.

## Dependencies

- Spec 37 (tenant columns + global filters + save-path guard)
- Implemented spec 06 (bulk conventions, batch caps, Dapper/SqlBulkCopy patterns)
- Spec 33 (offboarding purge semantics the bulk patterns must serve)
- Spec 38 (org-parameterized proc patterns)

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §1, §5 (purge/exit discipline)
- `backend/project-kit/context/data-layer.md` (bulk conventions)
- Original spec: `backend/project-kit/feature-specs/06-bulk-operations-and-advanced-data-handling.md`

## Agent Skills To Use

- `backend/.agents/skills/dapper-stored-procs/SKILL.md`
- `backend/.agents/skills/sql-server-2022/SKILL.md`
- `backend/.agents/skills/dotnet-ef-core/SKILL.md`

## Files Owned

- `Griot.Infrastructure/Repositories/BulkOperationRepository.cs` (org-guarded bulk paths)
- `Griot.Application/Services/BulkTaskService.cs` (batch pre-validation)
- `Griot.Infrastructure/Sql/` org-scoped bulk/purge T-SQL helpers (consumed by spec 33's purge job)
- SQL-gated xUnit bulk tests

## Implementation Notes

- Bulk status/update/move: one pre-validation `SELECT` (org-filtered, per spec 38's parameterized patterns) asserting **every** supplied task id belongs to `@OrganizationId` and the caller's permitted project set — a single foreign id rejects the **entire batch** (all-or-nothing, 403/404 semantics per spec 39 revision), never a partial write.
- All bulk write patterns (`SqlBulkCopy`, `MERGE`, batched `UPDATE`/`DELETE` with `IN` lists or temp-table joins) stamp `OrganizationId` from `ITenantContext` — never from client payload.
- Batch caps unchanged from spec 06 (same documented limits), plus a **per-org cap** so one suspended-volume org cannot starve the bulk pool.
- Purge patterns (for spec 33's offboarding job): org-scoped delete/anonymize executed in **chunks by `OrganizationId`**, idempotent and resumable (chunk progress recorded as `OrganizationLifecycleEvents`), cascade-safe order per the ERD (children before parents), user tombstone pattern per research §8.2/§8.3.
- Purge interplay with audit: app-path `IAuditService` rows (spec 20) are written before chunked deletes; DB triggers (spec 21) must not recurse on purge — trigger coverage tables verified during the SQL-gated purge tests.
- Activity/audit rows for bulk effects carry the org id and the spec-20 request-id chain, so one bulk op answers "what happened, where, how many" per tenant (spec 20's incident matrix).
- `ExecuteUpdateAsync`-style direct bulk statements (used by implemented auth/task paths) gain the org predicate in their `Where` — verified by a grep-able convention + SQL-gated test.
- Cross-tenant bulk attempt writes an `AuditLogs` row (`Bulk.CrossTenantRejected`) and returns 404 without disclosing the foreign rows' existence.

## Separation of Concerns

Batch validation + org predicate: `Griot.Application` services and `Griot.Infrastructure` repositories. Purge orchestration (retention window, lifecycle events, notifications) is spec 33 — this spec only supplies the reusable bulk primitives. Procs authorize nothing.

## Acceptance Criteria

- [ ] Batch containing one cross-org task id → zero effects, single rejection response (SQL-gated test)
- [ ] All bulk write paths stamp `OrganizationId` from the tenant context (foreign org id in payload ignored/rejected)
- [ ] Org-scoped chunked purge deletes/anonymizes every row of one org and **zero** rows of another (two-org seeded test)
- [ ] Purge re-run is idempotent (second pass no-ops, progress events recorded)
- [ ] Per-org bulk cap enforced without breaking the global batch cap contract
- [ ] `dotnet build` + SQL-gated `dotnet test` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.