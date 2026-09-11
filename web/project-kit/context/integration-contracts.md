# Integration Contracts (web → backend) — Multi-Tenant Addendum

> NOTE (2026-09-11): this file was created to host the web-side multi-tenant contract addendum.
> The root `project-kit/context/integration-contracts.md` remains the full cross-system surface;
> web route/type details also live in `web/project-kit/context/api-integration.md`.

## Multi-Tenant contract (PLANNED — 2026-09-11 multi-tenant migration wave)

Canonical: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`. Tenant = Organization (Company); pool model server-side; isolation is enforced in the backend (global query filters + repository guard) — the web never sends a tenant id.

**JWT v2 claims (access token, HS256, 15 min — backend 30, PLANNED):**
- Existing: `sub`, `email`, `jti`, `iss=Griot`, `aud=GriotClients`. New claims:
- `name` — `Users.DisplayName`
- `org` — active `OrganizationId` (set by `select-organization`; absent for platform-only SuperAdmin sessions)
- `role` — `super_admin` | organization role (`Owner`/`Admin`/`ProjectManager`/`Member`/`Client`) | `custom:{roleId}`
- `perms` — space-separated permission keys from the fixed catalogue: `org.read`, `org.settings.manage`, `org.members.manage`, `org.roles.manage`, `org.projects.view_all`, `project.manage`, `task.manage`, `comment.write`, `client.manage`, `client.feedback.read`, `client.feedback.respond`, `report.generate`, `log.read_tier`

**Refresh token:** stays opaque (64-hex random bytes, SHA-256 at rest, rotated, family-revoked) — **NOT a JWT**; the web never decodes it and jwt.io showing it blank is correct.

**Org switching:** `POST /api/auth/select-organization` → re-issues the full token pair for the selected company; the web updates the auth store, performs a full Apollo cache reset, and invalidates org-scoped TanStack queries.

**New REST routes consumed (PLANNED):**
- Organizations: `POST /api/organizations` (SuperAdmin onboarding; self-serve signup is later), `GET /api/organizations/{id}`, `POST /api/organizations/{id}/suspend`, `…/reactivate`, `…/offboard`, plan/metadata edits, `GET /api/organizations/{id}/lifecycle` (timeline events).
- Members/roles: `GET/POST/PUT/DELETE /api/organizations/{id}/members…` (+ invite chain) and `…/roles` (custom roles composing only the fixed permission catalogue).
- Client portal: client progress view (client-scoped reads over attached projects), `ClientFeedback` (create/list, status transitions, routed to the assigned PM), handoff acceptance.
- Handoff/maintenance: `ProjectHandoffs` checklist + status transitions, `HandoffDocuments` (blob-backed via backend 11), maintenance requests (Maintenance/PostDeploymentSupport).

**Client-view DTO shape note:** `ClientProjectViewDto` is a **projection, not the internal board shape** — percent-complete, milestones, recent-activity digest, feedback summary. The `Client` org role receives only this projection (server-enforced; mirrored by the AI capability gateway backend 25 / ai 13). The web renders DTOs and never reconstructs internal entities from it.

New/changed DTOs: `OrganizationDto`, `OrganizationMemberDto`, `ClientProjectViewDto`, `ClientFeedbackDto`, `ProjectHandoffDto`, `HandoffDocumentDto`, `OrganizationLifecycleEventDto`. Enums: `OrganizationStatus`, `OrganizationPlan` (display metadata only — no payments this wave), `OrganizationRole`, `PlatformRole`, `ClientFeedbackKind`, `ClientFeedbackStatus`, `HandoffStatus`, `HandoffDocumentKind`, `LifecycleEventKind`; `ProjectStatus` gains `Handoff`/`Maintenance`/`PostDeploymentSupport`.

All items PLANNED — they become implemented contract only when their owning spec ships on its own feature branch.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.