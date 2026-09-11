# Feature 14 — SuperAdmin Platform Console (Companies · Onboard/Offboard · Lifecycle)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · PLANNED — not implemented

## What This Delivers

The platform operator's console (SuperAdmin only, `role == super_admin`): the **all-companies directory** (search/filter/sort over every organization with status/plan/member-count), the **onboarding flow** (`POST /api/organizations` — create company + owner invite + seeds; self-serve signup stays out this wave), **suspend/reactivate/offboard flows** (offboard = export bundle → 30-day retention window → purge/anonymize, every step confirmed), and the **lifecycle event timeline** (`OrganizationLifecycleEvents`: Onboarded/Suspended/Reactivated/OffboardStarted/DataExported/Offboarded/Purged, per company).

## Dependencies

- Web features 03, 04, 05 (JWT v2 auth; a SuperAdmin platform session may carry no `org` claim).
- Backend specs 29 (foundation), 30 (JWT v2), 32 (onboard/manage), 33 (offboard/lifecycle) — PLANNED.
- Canonical contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` (§5 lifecycle).

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`
- `web/AGENTS.md` + `web/project-kit/context/{api-integration,design-system}.md`
- `docs/planning/RUNBOOK-ROLLBACK.md` (data-handling posture reference)

## Agent Skills To Use

- `web/.agents/skills/material-ui-theme/SKILL.md` (registry primitives, tokens only)
- `web/.agents/skills/tanstack-rest/SKILL.md` (platform-scoped REST hooks)

## Files Owned

- `web/src/features/platform/**` (company directory, onboard wizard, offboard flow, lifecycle timeline)
- Route: `/platform` (outside the org-scoped app shell rail)

## Implementation Notes

- Directory: company table/cards with `OrganizationStatus` chips (Active/Suspended/Offboarding/Archived), `OrganizationPlan` badges (metadata only), member counts, owner identity; search + status filter + sort.
- Onboard wizard: company name + unique slug + owner email → invite-by-email flow (branded Brevo template server-side); success lands the new row with an `Onboarded` timeline entry; seeds (system roles + default workspace) are server-side.
- Offboard flow: multi-step confirm (export → retention countdown → purge/anonymize) with a per-step confirmation card — never a single destructive click; export artifact downloads ride backend 11 blob links.
- Lifecycle timeline: vertical per-company event timeline (kind chip, actor, payload summary, monospace timestamp) — registry components, tokens only.
- Non-SuperAdmins never reach the route (web 05 guard on `role == super_admin`); every mutation is server-enforced.

## Separation of Concerns

- Presentation only; platform isolation is backend 29/32/33. This console reads company data only through the platform-scoped API as the SuperAdmin principal — it never embeds company-internal admin surfaces (web 13 owns those).

## Docker & Deploy

- No change (bundled into the Vercel build).

## Out of Scope

- Payments (deferred), SCIM 2.0 provisioning (deferred per research §12.4), company-internal member/role admin (web 13), AI/MCP platform tooling (ai 13+, mcp 06/07).

## Acceptance Criteria

- [ ] Only SuperAdmin reaches `/platform`; any other role is redirected (guard verified against the `role` claim)
- [ ] Onboard → suspend → reactivate → offboard flows work against backend 32/33 with confirmation cards at every destructive step
- [ ] Lifecycle timeline renders every `LifecycleEventKind` in order per company
- [ ] Directory search/filter/sort over all companies renders with status/plan metadata
- [ ] All states covered (empty/loading/error); `npm run lint && npm run typecheck && npm test && npm run build` green

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.