# Feature 48 — Multi-Tenant Revision of Feature 15 (Tasks & Comments) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 15; the original spec 15 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

Tasks and comments carry the tenant stamp (`OrganizationId`, backfilled), the **`comment.write`** permission key gates comment creation, and client feedback is deliberately routed into the separate **`ClientFeedback`** table (spec 34) — internal board/task internals and the `Comments` table are never exposed on the client surface.

## Dependencies

- Spec 37 (columns/filters), spec 34 (`ProjectClients`, `ClientFeedback` table)
- Implemented spec 15 (task CRUD/move, comment CRUD, mention handling ✅)
- Specs 30/31/39 (claims, `task.manage`/`comment.write` gates), spec 22 revision (org-scoped fan-out)

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §3, §6
- `backend/project-kit/context/data-layer.md` (`NotificationType`, comment conventions)
- Original spec: `backend/project-kit/feature-specs/15-tasks-comments.md`

## Agent Skills To Use

- `backend/.agents/skills/dotnet-ef-core/SKILL.md`
- Root `.agents/skills/contract-sync/SKILL.md`

## Files Owned

- `Griot.Application/Services/TaskService.cs`, `CommentService.cs` (org stamp + permission gates)
- `Griot.Api/Controllers/TaskController.cs`, `CommentController.cs` (attribute updates)
- Client-surface separation tests (xUnit: no internal DTO leaks; Postman via spec 43 revision)

## Implementation Notes

- `TaskItems` and `Comments` gain NOT NULL `OrganizationId` — backfilled transitively from the task's board→project chain (spec 37); create-time stamping from `ITenantContext` only.
- Comment create is gated by **`comment.write`** (`[RequirePermission]` REST + HotChocolate policy, spec 40 revision); comment update/delete keep their existing ownership/role rules and add the org scope through global filters — a client-role principal never passes `comment.write` on internal boards.
- **Client feedback separation:** client suggestions/edit-requests land in `ClientFeedback` (`Kind`: Comment/Suggestion/EditRequest; `Status`: New/Acknowledged/Resolved/Rejected) via the spec-34 routes and never in `Comments` — a contract test proves the client surface has no write path into `Comments`/task DTOs; internal boards never render `ClientFeedback` rows.
- Task CRUD/move/bulk-status gated by **`task.manage`**; assignment fan-out (assignee + mention notifications) resolves recipients **within the org** (`OrganizationMembers` active; spec 22 revision).
- Cross-tenant task id / comment id → **404** via global filters; a move or bulk operation referencing a foreign-org task is all-or-nothing rejected (spec 41 revision).
- Comment body/mentions conventions from implemented spec 15 unchanged; mention notification resolution now ANDs workspace membership with active org membership.
- Client progress reads never resolve raw task rows on the client surface — they go through `usp_GetClientProjectProgress` (spec 38 revision) / the dedicated progress service (spec 34).
- Suspended org: task/comment writes 403 `org_suspended`; reads available; OBO AI writes follow the same gate (spec 44 revision).
- Audit/activity rows for task/comment effects (spec 20 pipeline) carry `OrganizationId` automatically — no writer change beyond the stamp.

## Separation of Concerns

Task/comment domain logic stays in the implemented services; permission gates are attributes/policies (spec 39/40 revisions); client feedback is its own bounded context (spec 34) — this revision only adds the tenant stamp, the `comment.write` gate, and the separation guarantee.

## Acceptance Criteria

- [ ] Task/comment backfill + create-time org stamping verified (SQL-gated; child rows carry the task's org)
- [ ] Comment create without `comment.write` → 403; with it → 201 (permission matrix test incl. Custom roles)
- [ ] Client feedback lands in `ClientFeedback` only — contract test proves no client write path into `Comments` and no `ClientFeedback` row on internal boards
- [ ] Cross-org task/comment id → 404; foreign-org bulk batch fully rejected (all-or-nothing)
- [ ] Mentions/assignment fan-out resolves only active org members
- [ ] `dotnet build` + `dotnet test` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.