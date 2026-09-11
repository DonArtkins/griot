# Feature 15 — Client Portal UI (Progress · Feedback · Handoff Acceptance · Maintenance)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · PLANNED — not implemented

## What This Delivers

The client-facing portal: a **different progress view** for `Client`-org-role users over their `ProjectClients`-attached projects — percent-complete, milestones, recent-activity digest (never internal board internals beyond the granted `AccessLevel`, default `ProgressOnly`) — plus **feedback** (comment/suggest/edit-request on the project as `ClientFeedback`, routed to the assigned ProjectManager), **handoff acceptance** (review the handoff checklist + documents incl. the AI-generated manual, then accept), and **maintenance requests** once the project is in Maintenance/PostDeploymentSupport.

## Dependencies

- Web features 03, 04, 05 (JWT v2 auth + `Client` org-role route guards).
- Web feature 16 (PM-side handoff/maintenance authoring — the portal consumes its outputs).
- Backend specs 29 (foundation), 30 (JWT v2), 34 (client portal + feedback), 35 (handoff/maintenance) — PLANNED.
- Canonical contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` (§6 client support, §7 handoff/maintenance).

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`
- `web/AGENTS.md` + `web/project-kit/context/{api-integration,state-and-data,design-system}.md`
- `web/project-kit/feature-specs/16-project-handoff-maintenance-ui.md` (PM-side sibling)

## Agent Skills To Use

- `web/.agents/skills/material-ui-theme/SKILL.md` (registry primitives, tokens only)
- `web/.agents/skills/tanstack-rest/SKILL.md` (org-scoped REST hooks)

## Files Owned

- `web/src/features/clientPortal/**` (progress view, feedback threads, handoff acceptance, maintenance requests)
- Route: `/app/client` (+ `/app/client/projects/:id`, `/app/client/handoff/:id`, `/app/client/maintenance`)

## Implementation Notes

- Progress view consumes `ClientProjectViewDto` (percent-complete, milestone list, activity digest, feedback summary) — never board columns/task internals; milestone cards + progress bars from registry primitives.
- Feedback: composer with Kind selector (`Comment`/`Suggestion`/`EditRequest`); thread shows status chips (`New`/`Acknowledged`/`Resolved`/`Rejected`) + PM responses; the AI triage summary (ai 13) renders as a digest card when present.
- Handoff acceptance: read-only checklist view + `HandoffDocuments` list (downloads via backend 11 blob links; the `GeneratedByAi` manual previews in-app); the accept action posts client acceptance → the project advances toward maintenance.
- Maintenance: request form + list (status, responses) — client data is retained after offboarding, so the portal stays available for requests only.
- Clients never see the app shell's internal rail — the portal is a distinct route tree guarded on the `Client` org role (web 05).

## Separation of Concerns

- Presentation only; access level and feedback routing are server-side (backend 34/35 + capability gateway backend 25 / ai 13). The portal renders DTOs and never reconstructs internal entities from the client projection.

## Docker & Deploy

- No change (bundled into the Vercel build).

## Out of Scope

- PM handoff authoring/checklists (web 16), internal boards (web 08), AI triage logic (ai 13), payments.

## Acceptance Criteria

- [ ] Client sees only `ProjectClients`-attached projects in progress-view shape — no board internals render for the `Client` role (route guard + 403 verified)
- [ ] Feedback (all three kinds) posts, routes to the assigned PM as a notification, and status transitions render with PM responses
- [ ] Handoff: documents download, the AI manual previews in-app, acceptance posts and the project state advances
- [ ] Maintenance requests submit + list while the project is in Maintenance/PostDeploymentSupport
- [ ] All states covered (empty/loading/error); `npm run lint && npm run typecheck && npm test && npm run build` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.