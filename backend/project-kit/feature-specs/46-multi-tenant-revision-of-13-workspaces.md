# Feature 46 — Multi-Tenant Revision of Feature 13 (Workspaces, Members & Invites) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 13; the original spec 13 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

Workspaces move under organizations: `Workspaces.OrganizationId` (NOT NULL, stamped from the tenant context), membership checks AND-ed with org membership, the **member DTO completeness fix** on `POST /api/workspaces` (returns `displayName`/`email`/`avatarUrl` exactly like GET), and the **organization invite chain** (`OrganizationInvites`) layered above the existing workspace invites.

## Dependencies

- Spec 37 (tenant columns/filters), spec 29 (org + member tables)
- Implemented spec 13 (workspace CRUD, `WorkspaceMembers`, invite-token flow ✅)
- Specs 30/31/39 (JWT `org`/`role`/`perms`, permission gates, suspend semantics)

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §2–3
- `docs/api/auth-contract.md` (select-organization — the org a workspace create runs in)
- Original spec: `backend/project-kit/feature-specs/13-workspaces-members-invites.md`

## Agent Skills To Use

- `backend/.agents/skills/dotnet-ef-core/SKILL.md`
- Root `.agents/skills/contract-sync/SKILL.md`

## Files Owned

- `Griot.Application/Services/WorkspaceService.cs` (org stamp + completeness fix), `OrganizationInviteService.cs`
- `Griot.Api/Controllers/WorkspaceController.cs` + new `OrganizationInviteController.cs` routes
- xUnit + Postman (spec 43 revision) coverage for the org-scoped workspace surface

## Implementation Notes

- `Workspaces.OrganizationId` is **NOT NULL** and stamped at create from `ITenantContext` (JWT `org`) — backfilled to the bootstrap default org by the spec-37 migration; client-supplied org ids are always ignored.
- Workspace membership resolution now requires **`OrganizationMembers.Status = Active`** for the caller's org in addition to the existing `WorkspaceMembers` role checks — a removed/suspended org member loses workspace access even if their workspace row survives (orphaned workspace rows are cleaned by spec 33's purge).
- **Member DTO completeness fix:** `CreateWorkspaceAsync` reloads the owner `WorkspaceMember` with `.Include(m => m.User)` (or hydrates from the user repo) before mapping so `POST /api/workspaces` returns `displayName`/`email`/`avatarUrl` identically to `GET /api/workspaces` — the known Postman defect, also acceptance in specs 29/39.
- Organization invite chain (PLANNED): `POST /api/organizations/{id}/invites` (company Admin, `org.members.manage`) → `OrganizationInvites` row (email, `OrganizationRole`, optional `CustomRoleId`, token, expiry); `GET /api/invites/{token}` + `POST /api/invites/{token}/accept` extended to accept org invites → on accept, `OrganizationMembers(Status=Active)` row + Brevo `organization_invite` mail (spec 45 revision).
- Invite scoping rule: a workspace invite may only target a user who is already an **active org member** (or arrives via the org invite chain) — cross-org workspace invites are rejected 403; the two invite chains remain separate tables with separate tokens.
- Existing workspace routes (`GET/PUT/DELETE /api/workspaces`, member add/PATCH/DELETE, workspace invites) keep their role model (Owner/Admin/Member) and gain the org scope through global filters — no cross-org id resolves to 404.
- Members listing paginated per the spec-18 contract (`page`/`pageSize`/whitelisted sort on `CreatedAt`/`DisplayName`, `role` filter, `q` over name/email); DTOs gain `organizationRole` alongside the workspace role.
- `OrganizationRole.Custom` members carry `CustomRoleId` → `Roles` permissions evaluated per spec 31; `org.members.manage` gates member add/role-change/remove.
- Suspend interplay: suspended org → workspace writes 403 `org_suspended` (read lists stay available per spec 32).

## Separation of Concerns

Org invite chain: `Griot.Application` services + new routes. Workspace role model stays `WorkspaceMembers`-owned; org role model stays `OrganizationMembers`-owned — the two are composed at authorization time, never merged. Email transport is spec 45's revision.

## Acceptance Criteria

- [ ] Workspace create stamps `OrganizationId` from the tenant context; client-supplied org id ignored (test)
- [ ] Removed/suspended org member loses workspace access (authorization test with active workspace row)
- [ ] `POST /api/workspaces` returns `displayName`/`email`/`avatarUrl` identical to `GET /api/workspaces` (fix locked by Postman + xUnit)
- [ ] Org invite → accept → `OrganizationMembers(Active)`; cross-org workspace invite rejected 403
- [ ] Members list follows the spec-18 pagination contract; cross-org id → 404
- [ ] `dotnet build` + `dotnet test` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.