# Feature 13 — Company Admin Console (Members · Custom Roles · All-Projects · Settings)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · PLANNED — not implemented

## What This Delivers

The Company Admin (Owner/Admin) console inside the App shell: company **member management** (invite via the company-scoped invite chain, suspend/reactivate, role assignment incl. custom roles), a **custom-role builder** (composing only the fixed permission catalogue), an **all-projects view inside their own company** (every project under the company, per role scope), and **company settings** (rename, plan metadata display, ownership transfer). Everything is scoped to the admin's own company only — the server is the isolation boundary; the UI mirrors it, never enforces it.

## Dependencies

- Web features 03, 04, 05 (REST/GraphQL clients + JWT v2 auth with `org`/`role`/`perms` claims).
- Web feature 07 (app shell + rail entry).
- Backend specs 29 (multi-tenant foundation), 30 (JWT v2), 31 (org members/roles routes) — PLANNED.
- Canonical contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` (§2 entities, §3 role model, §4 JWT v2).

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`
- `web/AGENTS.md` + `web/project-kit/context/{api-integration,state-and-data,design-system}.md`
- `web/project-kit/feature-specs/05-secure-auth-and-state-management.md` (JWT v2 section)

## Agent Skills To Use

- `web/.agents/skills/material-ui-theme/SKILL.md` (registry primitives, tokens only)
- `web/.agents/skills/tanstack-rest/SKILL.md` (org-scoped REST hooks)

## Files Owned

- `web/src/features/companyAdmin/**` (members console, role builder, all-projects view, settings)
- Route: `/app/company` (+ `/app/company/roles`, `/app/company/projects`, `/app/company/settings`)

## Implementation Notes

- Members: list `OrganizationMembers` (role chip, status `Invited/Active/Suspended`), invite by email (`OrganizationInvites`; branded Brevo template is server-side), suspend/reactivate, assign a system or custom role.
- Custom-role builder: create/edit `Roles` inside the company composing only the fixed permission catalogue keys (`org.read`, `org.settings.manage`, `org.members.manage`, `org.roles.manage`, `org.projects.view_all`, `project.manage`, `task.manage`, `comment.write`, `client.manage`, `client.feedback.read`, `client.feedback.respond`, `report.generate`) — no platform or lifecycle powers, ever; the checkbox list groups keys by domain and disables keys the acting admin lacks.
- All-projects view: every project under the company (server returns all when `perms` includes `org.projects.view_all`); reuses `ProjectCard` from the registry; per-project badges show assigned PMs and attached clients.
- Settings: rename company, display `OrganizationPlan` (Free/Pro/Enterprise — metadata only), ownership transfer behind a confirm card, suspended-state banner (`403 org_suspended` → read-only mode).
- Every surface ships EmptyState/LoadingSkeleton/ErrorState; optimistic member/role edits with rollback + toast.

## Separation of Concerns

- Presentation only: this console composes registry components + REST hooks; all authorization and tenant isolation is server-side (backend 29 global query filters + repository guard). No backend code, no direct DB concepts beyond DTOs.

## Docker & Deploy

- No change: bundled into the existing Vercel build (infra spec 01). No new env/ports.

## Out of Scope

- Platform-level onboarding/suspension/offboarding (web 14), client portal (web 15), payments/billing (deferred), workspace-level invite UI (web 07 unchanged).

## Acceptance Criteria

- [ ] Owner/Admin sees all projects in their company only; a user from another company sees nothing of this console (route guard + 403 handling verified)
- [ ] Invite → accept → role assign → suspend/reactivate works end-to-end against the org member routes
- [ ] Custom roles compose only the fixed permission keys; role changes re-issue `perms` on the next token (verify the claims-refresh path)
- [ ] Company settings rename + plan display render; a suspended company shows the read-only banner and blocks writes
- [ ] All states covered (empty/loading/error); `npm run lint && npm run typecheck && npm test && npm run build` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.