# Feature 16 — Project Handoff & Maintenance UI (PM Side)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · PLANNED — not implemented

## What This Delivers

The PM/Admin close-out surface: **handoff checklist** authoring per project (`ProjectHandoffs`: deliverables, credentials, environments, docs — status `NotStarted → InProgress → AwaitingClientAcceptance → Completed`), **handoff documents** (browse/upload `HandoffDocuments`, incl. the AI-generated client user manual — `Kind = Manual`, `GeneratedByAi = true`; ai 14 generates, this UI renders/exports), **client offboarding** (narrow client access to portal-read-only + maintenance requests), and the **maintenance queue** (support requests from the client in Maintenance/PostDeploymentSupport — respond/close, optional AI triage digests).

## Dependencies

- Web features 03, 04, 05, 07 (auth + app shell).
- Web feature 15 (client portal consumes handoff + maintenance outputs).
- Backend specs 29/30/35 — PLANNED; blob storage backend 11 for document artifacts; ai 14 for the generated manual.
- Canonical contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` (§7 handoff/client offboarding/maintenance).

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`
- `web/AGENTS.md` + `web/project-kit/context/{api-integration,state-and-data,design-system}.md`
- `web/project-kit/feature-specs/15-client-portal-ui.md` (client-side sibling)

## Agent Skills To Use

- `web/.agents/skills/material-ui-theme/SKILL.md` (registry primitives, tokens only)
- `web/.agents/skills/tanstack-rest/SKILL.md` (org-scoped REST hooks)

## Files Owned

- `web/src/features/handoff/**` (checklist editor, documents manager, offboard step, maintenance queue)
- Route: `/app/projects/:id/handoff` (+ `/app/maintenance` queue)

## Implementation Notes

- Checklist editor: sectioned checklist persisted as `ChecklistJson` (deliverables/credentials/environments/docs) with a progress indicator; status transitions behind confirm cards — "send for client acceptance" is always an explicit step.
- Documents manager: browse/upload `HandoffDocuments` (kind chips `Manual`/`Credential`/`Design`/`Report`/`Other`, `GeneratedByAi` badge); the AI manual arrives from ai 14 → preview + export; credential entries render masked with reveal-by-confirm.
- Client offboarding: select clients → confirm card → access narrows to portal read-only + maintenance (server-enforced); client data is retained — never purged here (company offboarding is web 14's platform flow).
- Maintenance queue: client requests list with status + response composer; AI triage digest cards (ai 14) optional; resolve/close updates the project state.
- Org-scoped everywhere: mutations ride the org-scoped JWT v2 token; `ProjectStatus` chips show `Handoff`/`Maintenance`/`PostDeploymentSupport` states.

## Separation of Concerns

- Presentation only; the handoff state machine, blob storage and AI generation live in backend 11/35 + ai 14. This UI renders DTOs and posts state transitions only.

## Docker & Deploy

- No change (bundled into the Vercel build).

## Out of Scope

- Client-side portal (web 15), company offboarding (web 14), AI manual generation logic (ai 14), payments.

## Acceptance Criteria

- [ ] Checklist authoring + status transitions persist; the project reaches `AwaitingClientAcceptance` and the client is notified
- [ ] Documents upload/list/download (incl. the AI manual with the `GeneratedByAi` badge); credential entries stay masked until a confirmed reveal
- [ ] Client offboarding narrows client access and requires confirmation; client data remains retained for maintenance
- [ ] Maintenance queue: respond/close a client request end-to-end; `Handoff`/`Maintenance`/`PostDeploymentSupport` project states render correctly
- [ ] All states covered (empty/loading/error); `npm run lint && npm run typecheck && npm test && npm run build` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.