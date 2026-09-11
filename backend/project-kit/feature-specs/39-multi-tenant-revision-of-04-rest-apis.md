# Feature 39 — Multi-Tenant Revision of Feature 04 (REST APIs in ASP.NET Core 8) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 04; the original spec 04 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

Org-scoped REST across the whole implemented surface: permission attributes on all controllers, org resolution through `TenantResolutionMiddleware` (JWT `org` claim only), the **403 `org_suspended`** behavior for suspended/offboarding companies, and the **`POST /api/workspaces` member user-data completeness fix** (response must return `displayName`/`email`/`avatarUrl` exactly like `GET /api/workspaces`).

## Dependencies

- Specs 29 (schema/tenant context), 30 (JWT v2 `org`/`role`/`perms` claims), 31 (permission catalogue + `[RequirePermission]`)
- Implemented spec 04 (controllers, `DomainControllerBase`, `DomainError` mapping)
- Spec 32 (suspend semantics that produce `org_suspended`)

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §1, §3, §5
- `docs/api/auth-contract.md` (JWT v2 PLANNED section)
- Original spec: `backend/project-kit/feature-specs/04-rest-apis-dotnet8.md`
- `backend/project-kit/context/api-surface.md`

## Agent Skills To Use

- `backend/.agents/skills/jwt-argon2-auth/SKILL.md`
- Root `.agents/skills/contract-sync/SKILL.md`
- `backend/.agents/skills/dotnet-ef-core/SKILL.md`

## Files Owned

- `Griot.Api/Middleware/TenantResolutionMiddleware.cs` (+ suspend-gate filter)
- `Griot.Api/Authorization/RequirePermissionAttribute.cs` wiring on every controller
- `Griot.Api/Controllers/*` (attribute + org-scope updates), `Griot.Application/Services/DomainService.cs` (workspace create fix)
- `backend/project-kit/context/api-surface.md` route conventions update

## Implementation Notes

- `TenantResolutionMiddleware` resolves `ITenantContext` **only** from the validated JWT `org` claim — never from a header, query string or body; unauthenticated/platform requests run with `WithTenantScope(null)` and are audit-logged.
- Every mutating endpoint carries `[RequirePermission("<key>")]` from the spec-31 catalogue (`org.read, org.settings.manage, org.members.manage, org.roles.manage, org.projects.view_all, project.manage, task.manage, comment.write, client.manage, client.feedback.read, client.feedback.respond, report.generate, log.read_tier`); the `perms` claim is an optimization — **authorization is re-checked server-side**.
- Suspend gate: when the active `Organizations.Status` is `Suspended` or `Offboarding`, all write verbs return **403** `application/problem+json` with code `org_suspended` (reads and auth stay allowed — spec 32 contract).
- Cross-tenant access: an org-scoped id that exists in another organization resolves through global query filters to "not found" → **404** (never 403, which would leak existence); only same-org privilege failures return 403.
- **`POST /api/workspaces` fix:** `CreateWorkspaceAsync` must reload the owner `WorkspaceMember` with `.Include(m => m.User)` (or hydrate from the user repo) before mapping, so the response returns the member's `displayName`/`email`/`avatarUrl` identically to `GET /api/workspaces` — acceptance evidence in this spec and in Postman (spec 43 revision).
- DTOs gain `organizationId` where clients need it (workspace/project/task read models); `OrganizationRole`-aware role fields replace bare `WorkspaceRole` in member DTOs where the tenant wave changes semantics (specs 46–48 revisions).
- New route families from the wave are owned by their own specs (29/32/33/34/35) — this spec owns the **cross-cutting conventions** they must follow (auth shape, error envelope, permission attributes, org resolution).
- `DomainControllerBase` gains helpers: `CurrentOrgId`, `CurrentRole`, `HasPermission(key)`; controllers stay thin (no tenant logic inline).
- AI OBO requests (spec 09/44 revision) flow through the same middleware; the OBO org is bound to the delegation's `OrganizationId`.

## Separation of Concerns

Tenant resolution + suspend gate: `Griot.Api` middleware. Permission policy + DTO mapping: `Griot.Application`. Persistence-level isolation: EF filters (spec 37). Controllers never resolve orgs themselves and never see raw claims beyond the principal.

## Acceptance Criteria

- [ ] Every mutating REST endpoint is permission-attributed; a member without the key gets 403 even with a stale-but-valid token claim (server-side re-check test)
- [ ] Suspended org: any write → 403 `org_suspended`; reads/auth still 200 (integration test)
- [ ] Cross-tenant id → 404 (two-org seeded integration test); no existence leak
- [ ] `POST /api/workspaces` returns member `displayName`/`email`/`avatarUrl` identical to `GET /api/workspaces` (Postman + xUnit assertion)
- [ ] Token without `org` claim (pre-v2 token) is rejected or treated per the auth-contract PLANNED section — documented + tested
- [ ] `dotnet build` + `dotnet test` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.