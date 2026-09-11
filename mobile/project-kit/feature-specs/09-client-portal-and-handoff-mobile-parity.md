# Feature 09 — Client Portal & Handoff (Mobile Parity)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented**

## What This Delivers

The client-facing mobile surface for a project's satisfaction-first portal: the progress view (percent-complete, milestones, recent-activity digest — no internal board internals beyond the granted level), feedback/suggestions (`ClientFeedback`: comment, suggestion, edit request), handoff acceptance (`ProjectHandoffs` checklist → client accepts), and maintenance/post-deployment support requests after offboarding. Parity with web 15/16; backend owners are specs 34/35.

## Dependencies

- Mobile features 02–08 (auth + JWT v2 role navigation, dio/GraphQL, Riverpod, responsive shells, org switching).
- Backend spec 34 (client portal: `ProjectClients`, `ClientFeedback`), spec 35 (handoff + maintenance), spec 22 (org-scoped notifications).
- Web specs 15 (client portal) and 16 (handoff/maintenance) — reference UIs; mobile parity is feature-complete, not pixel-identical.

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` (§6 client support surface, §7 handoff/maintenance)
- `mobile/project-kit/context/integration-contracts.md` (Multi-Tenant contract, PLANNED)
- `mobile/project-kit/context/{design-system,api-integration}.md`

## Agent Skills To Use

- `mobile/.agents/skills/dio-rest/SKILL.md`
- `mobile/.agents/skills/riverpod-state/SKILL.md`
- `mobile/.agents/skills/graphql-flutter/SKILL.md`

## Files Owned

- `mobile/lib/features/client-portal/**` (progress view, feedback thread, handoff acceptance, maintenance requests)

## Implementation Notes

- Progress view reads client-scoped endpoints/GraphQL fields only (percent-complete, milestones, activity digest); board internals are never fetched or rendered for a `Client` session (enforced server-side by backend 34).
- Feedback: compose `Comment`/`Suggestion`/`EditRequest` items (`ClientFeedback`); the thread shows the PM's public `PmResponse` and status transitions (`New` → `Acknowledged` → `Resolved`/`Rejected`); AI triage summaries (ai 13) arrive through backend notifications only.
- Handoff: display the `ProjectHandoffs` checklist and handoff documents (`HandoffDocuments` — manual/credentials/design/report kinds), and submit client acceptance (status `AwaitingClientAcceptance` → `Completed`).
- Maintenance: after client offboarding (access narrowed to portal-read-only), raise and track maintenance/post-deployment support requests; project status `Maintenance`/`PostDeploymentSupport`.
- All data is org-scoped from the token `org` claim; a client sees only projects where they are attached via `ProjectClients`.

## Separation of Concerns

- The client portal is its own feature folder; it consumes the same `core/` transport as the rest of the app; no board/task widgets are reused into the client surface.

## Docker & Deploy

- Local run on emulator against compose backend. CI APK artifact unchanged (infra 05).

## Out of Scope

- PM-side feedback triage dashboards (web 15 + ai 13).
- Push notifications (v2).
- Client billing/payments (never in this wave).

## Acceptance Criteria

- [ ] A `Client` session renders the progress view (percent, milestones, digest) with no board internals reachable via the app
- [ ] Client can create feedback items and see status + PM responses; submissions persist via REST
- [ ] Handoff checklist + documents render and client acceptance flips the handoff status
- [ ] Maintenance requests can be raised and tracked post-offboarding (portal-read-only access preserved)
- [ ] Client sees only `ProjectClients`-attached projects in the active org

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.