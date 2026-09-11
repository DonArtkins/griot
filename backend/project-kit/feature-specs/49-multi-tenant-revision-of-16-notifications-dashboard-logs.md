# Feature 49 — Multi-Tenant Revision of Feature 16 (Notifications, Dashboard & Logs) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 16; the original spec 16 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

The notification/dashboard/logs read surface under the tenant stamp: `Notifications.OrganizationId` (org-scoped user notifications + **client notifications** for handoff/feedback events), dashboard aggregates computed **per-org** (Admin org-wide, PM assigned-projects), and org-stamped log rows whose per-tenant reads feed the spec-25 tier matrix.

## Dependencies

- Spec 37 (columns/filters), spec 34/35 (client events), specs 32/33 (lifecycle recipients)
- Implemented spec 16 (notification routes, dashboard summary, activity, logs ✅)
- Specs 18/19 (query contract, cache keys), spec 20 (log writers), spec 25 (tiered access consumer)

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §1, §5–7
- `docs/observability/HOW-LOGGING-WORKS.md` (log tables/runtime — spec 20)
- Original spec: `backend/project-kit/feature-specs/16-notifications-dashboard-logs.md`

## Agent Skills To Use

- `backend/.agents/skills/dapper-stored-procs/SKILL.md` (dashboard proc)
- `backend/.agents/skills/dotnet-ef-core/SKILL.md`
- Root `.agents/skills/contract-sync/SKILL.md`

## Files Owned

- `Griot.Application/Services/NotificationService.cs`, `DashboardService.cs` (org scoping + client notification types)
- `Griot.Api/Controllers/NotificationsController.cs`, `DashboardController.cs`, logs controllers (org-aware gates)
- Dashboard/log org-scoping tests (xUnit; Postman via spec 43 revision)

## Implementation Notes

- `Notifications` gains NOT NULL `OrganizationId` (backfilled to the bootstrap default org by spec 37; every new row stamped from `ITenantContext` at fan-out — spec 22 revision is the writer).
- Notification list/unread-count/read-all keep their **user scoping** and gain org scoping via global filters; a user in two orgs sees only notifications of the **active** `org` claim (org switch changes the view — spec 42 revision).
- **Client notifications** (PLANNED, `NotificationType` gains per the spec-22 revision): handoff `AwaitingClientAcceptance` → client; PM response on `ClientFeedback` → client; client feedback posted → assigned PM (specs 34/35 events). Clients resolve these through the client surface only.
- **Dashboard per-org:** `usp_GetDashboardSummary` gains `@OrganizationId` (spec 38 revision) — company **Admin/Owner** sees org-wide aggregates (all projects in the org, `org.projects.view_all`), **PM** sees assigned-projects aggregates, **Member** sees their workspaces as today; caching under `cache:org:{orgId}:…` (spec 19 revision).
- **Logs org-stamped:** `ApiLogs`/`ErrorLogs`/`AuditLogs`/`ActivityLogs` gain nullable `OrganizationId` (null = platform-level; spec 51 revision owns the writer changes) + index; implemented role gates on `GET /api/logs/errors`/`/audit` remain, with per-tenant reads resolved by **spec 25**'s tier matrix (raw rows stay SuperAdmin/Dev — this spec adds the org dimension, not new access grants).
- Activity feed (`GET /api/workspaces/{id}/activity`) inherits org scoping via filters; org-role-aware visibility unchanged from implemented membership rules.
- Suspended/offboarding org: notification **writes** blocked with 403 `org_suspended` (offboarding export/purge notices are produced by the lifecycle services, specs 32/33, before the block); reads available.
- Dashboard cache invalidation keys move to the org-prefixed convention (spec 19 revision); `X-Cache` behavior unchanged.
- Log query extensions (`fixStatus`, `entityType`, `actorId`, sorting) keep their spec-18/20 contracts, now org-parameterized in the Dapper paths.

## Separation of Concerns

Fan-out writers are spec 22's revision; log writers are spec 20/51; this revision owns the **read surface** (scoping, org aggregates, client notification types). Access tiers to log data are spec 25's policy, enforced at these controllers.

## Acceptance Criteria

- [ ] Notifications org-scoped: a dual-org user sees only the active org's notifications; backfill verified
- [ ] Client events produce client notifications routed to the correct PM/client recipients (handoff + feedback tests)
- [ ] Dashboard returns org-wide aggregates for Admin, assigned-projects for PM, member-scoped for Member (three-role matrix test)
- [ ] Log rows carry `OrganizationId` (null platform); per-tenant log reads respect the spec-25 tier contract
- [ ] Suspended-org notification writes → 403 `org_suspended`
- [ ] `dotnet build` + `dotnet test` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.