# Feature 40 — Multi-Tenant Revision of Feature 05 (GraphQL Layer — HotChocolate) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 05; the original spec 05 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

Org context in GraphQL via the tenant context: every resolver's EF reads run through the spec-37 global query filters bound to `ITenantContext` (JWT `org`), field-level permission policies from the spec-31 catalogue guard mutations, and dedicated **read-only client-view types** are added so the client progress surface never exposes internal board/task internals.

## Dependencies

- Specs 29/30/31 (tenant context, JWT `org`/`role`/`perms`, permission policies)
- Spec 37 (global query filters the resolvers rely on)
- Implemented spec 05 (HotChocolate 14+ conventions, `AiFieldMiddleware`, DataLoader)
- Spec 34 (client-view DTO shape this layer mirrors)

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §1, §3, §6
- `backend/project-kit/context/api-surface.md` (GraphQL conventions)
- Original spec: `backend/project-kit/feature-specs/05-graphql-layer-hotchocolate.md`

## Agent Skills To Use

- `backend/.agents/skills/hotchocolate-graphql/SKILL.md`
- Root `.agents/skills/contract-sync/SKILL.md`

## Files Owned

- `Griot.Api/GraphQL/TenantRequestContext.cs` (org binding into the request context)
- `Griot.Api/GraphQL/ClientViewTypes.cs` (read-only client-view types)
- `Griot.Api/Program.cs` (field-level policy registration), existing `GriotMutation`/query types (org-scope updates)
- Postman/Newman GraphQL test updates (spec 43 revision)

## Implementation Notes

- `ITenantContext` is bound into the HotChocolate request context at execution start; every resolver that queries a tenant entity relies on the **global query filters** (spec 37) — resolvers never hand-write an org `Where` they can forget, and DataLoader batching stays org-scoped automatically.
- Field-level authorization: mutations carry HotChocolate policies mapped from the spec-31 permission keys (`task.manage`, `project.manage`, `client.manage`, …) reading the `perms` claim — with **server-side re-check** against `OrganizationMembers`/`Roles` in the application service (claims are an optimization, never the policy source of truth).
- Client-view types (`ClientProjectView`, `ClientProgressDigest`, `ClientFeedbackType`) are **query-only** (no mutation mapping) and constructed from the dedicated `ClientProjectViewDto` — never the internal board/task DTOs; a contract test asserts no internal field leaks into the schema.
- `AiFieldMiddleware` default-deny is **unchanged**: unmapped operations stay 403; OBO context (spec 44 revision) binds the delegation's `OrganizationId` into the tenant scope so AI reads resolve inside one org.
- Denial semantics preserved: GraphQL denials are errors with **HTTP 200** for `application/json` (REST denials remain 403 per spec 39 revision).
- Suspended/offboarding orgs: mutation resolvers reject with the `org_suspended` problem code surfaced as a GraphQL error (read queries still resolve).
- New tenant types added to the schema: `Organization`, `OrganizationMember`, `Role`, `ClientFeedback`, `ProjectHandoff`, `OrganizationLifecycleEvent` — queries paginated per the spec-18 contract (typed `OrganizationFilterInput`/`SortInput` mirroring REST, zero-drift rule).
- SuperAdmin platform queries (all orgs) run only via the documented `WithTenantScope(null)` bypass inside the service layer, never in resolver code.
- Cost/complexity limits (spec 19) unchanged; client-view queries carry the same depth limits.

## Separation of Concerns

GraphQL never resolves orgs or authorizes role tiers itself — it maps HTTP context + claims to policies and delegates to `Griot.Application` services which re-check. Persistence isolation is EF/SQL layer (specs 37/38). AI boundaries stay in the existing middleware.

## Acceptance Criteria

- [ ] Cross-org GraphQL query returns no rows (two-org seeded test) without any resolver-level `Where`
- [ ] Mutation without the required permission policy → denied; bypassing the policy via a forged claim still 403 (server-side re-check test)
- [ ] Client-view types are query-only; schema assertion proves no internal board/task field appears in them
- [ ] OBO AI call to an unmapped operation still denied by `AiFieldMiddleware` (default-deny unchanged)
- [ ] GraphQL denials remain errors with HTTP 200 for `application/json`
- [ ] `dotnet build` + `dotnet test` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.