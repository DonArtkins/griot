# Feature 51 — Multi-Tenant Revision of Feature 20 (Observability & Logging Pipeline) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 20; the original spec 20 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

The implemented logging pipeline (ApiLoggingMiddleware → `ApiLogs`, exception handler → `ErrorLogs`, `IAuditService` → `AuditLogs`/`ActivityLogs`) gains **nullable `OrganizationId`** on all four tables plus an index, so **per-tenant log reads** are possible (feeding spec 25's role-tiered access and mcp 07). The `usp_PruneObservabilityLogs` retention contract is unchanged; every audit row carries the org of the event it records.

## Dependencies

- Implemented spec 20 (pipeline ✅ — `docs/observability/HOW-LOGGING-WORKS.md`; its planned durable-dispatch gate stays open)
- Spec 37 (migration conventions, tenant-context availability at write time)
- Spec 25 revision (per-tenant tiered reads), spec 33 (offboard/purge audit trail)

## Context To Read First

- `docs/observability/HOW-LOGGING-WORKS.md` + `docs/observability/LOGGING-AUDIT-REPORT.md`
- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §1 (org column on log tables is the guide's contract)
- Original spec: `backend/project-kit/feature-specs/20-observability-logging-pipeline.md`

## Agent Skills To Use

- `backend/.agents/skills/dotnet-ef-core/SKILL.md`
- `backend/.agents/skills/sql-server-2022/SKILL.md`
- Root `.agents/skills/contract-sync/SKILL.md`

## Files Owned

- Migration adding nullable `OrganizationId` + `IX_{table}_OrganizationId` on `ApiLogs`, `ErrorLogs`, `AuditLogs`, `ActivityLogs`
- `Griot.Api/Middleware/ApiLoggingMiddleware.cs`, `IErrorLogService`, `IAuditService` (org stamp at write)
- `usp_PruneObservabilityLogs` (org column carried through; logic unchanged)
- Per-tenant log-read tests (xUnit; SQL-gated where applicable)

## Implementation Notes

- Migration adds **nullable `OrganizationId`** (uuid) to `ApiLogs`, `ErrorLogs`, `AuditLogs`, `ActivityLogs` — null means **platform-level/system event** (unauthenticated requests, SuperAdmin platform actions, outbox workers); NOT NULL is impossible because early-exit 401/404/429 rows precede tenant resolution by design.
- Writer changes (stamp-only, semantics untouched): `ApiLoggingMiddleware`'s `TelemetryWriter` sets the org from `ITenantContext` after authentication resolves (late binding on response completion, same place `UserId` is read); `IErrorLogService.RecordAsync` and `IAuditService` stamp the active org; `DomainService` mutation audit rows carry the domain row's org.
- Audit rows **carry org**: every `AuditLogs`/`ActivityLogs` row produced for org-scoped effects (task/project/member/invite/notification/lifecycle) records that org; spec 32/33's `OrganizationLifecycleEvents` remain the dedicated lifecycle table (not merged into `AuditLogs`).
- Index `IX_{table}_OrganizationId` on all four tables (filtered where NULL-heavy, per SQL Server 2022 conventions) so per-tenant log queries don't scan platform rows.
- **Retention unchanged:** `usp_PruneObservabilityLogs` keeps the 91/366-day windows and idempotent re-run — it carries the new column through untouched; the existing prune acceptance (`ObservabilityPruneSqlTests`) must still pass.
- **Read side:** per-tenant log reads (org filter) resolve through spec 25's tier matrix — org Admin gets the sanitized audit projection within own org only; **raw logs stay SuperAdmin/Dev**; platform (NULL-org) rows never leak to org readers.
- IP masking, `QueryString` redaction, `DurationMs > 0` clamp, RFC 7807 responses, best-effort isolation (telemetry loss never fails a committed request), same-transaction mandatory audit/activity persistence — **all implemented behavior unchanged**; this revision adds the stamp and the org-scoped read contract.
- Incident-answer matrix (spec 20's contract) gains the tenant dimension: "affected spillover / how many users" queries filter by `OrganizationId` + `RequestId` window.
- Postman folder 14 audit assertions extended to assert org presence on org-scoped mutations (spec 43 revision).

## Separation of Concerns

Writers stay in the implemented pipeline components (stamp-only); tiered **access** is spec 25's policy at the log controllers; **retention** stays the prune proc; observability tables never gain triggers (spec 21 guard). Platform (null-org) semantics are documented, not inferred.

## Acceptance Criteria

- [ ] All four log tables carry nullable `OrganizationId` + index; org-stamped rows verified for authed requests, NULL rows for unauthenticated/platform paths
- [ ] Per-tenant log query returns only the active org's rows; platform rows never leak to org readers (SQL-gated + xUnit)
- [ ] `usp_PruneObservabilityLogs` unchanged behavior proven (existing prune tests green, org column carried through)
- [ ] Implemented spec-20 acceptance (RFC 7807, masking, redaction, `DurationMs > 0`, audit same-transaction) all still green
- [ ] Incident queries answer "which tenant / affected spillover / user counts" per org (query recipe test)
- [ ] `dotnet build` + `dotnet test` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.