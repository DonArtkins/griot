# Feature 09 - Automated UI Testing (Cypress)

## Type

NEW FEATURE

## What This Delivers

The Week-6 step 6 deliverable: automated UI testing via Cypress (Selenium documented as alternative) - the core-loop E2E suite plus Copilot flows with the stubbed copilot.

## Dependencies

- Web features 05-10 (app works).
- Backend deployed or CI API service.

## Context To Read First

- `research/week-06-quality-engineering-foundations.md` sec on Cypress

## Agent Skills To Use

- `qa/.agents/skills/cypress-e2e/SKILL.md`

## Files Owned

- `web/cypress/**`, workflow `cypress` job (with infra)

## Files

CREATE: `web/cypress/e2e/core-loop.cy.ts`, `copilot.cy.ts` (MSW stub), support config with data-testid selectors.

## Implementation Notes

- Core loop: signup, create project, add 3 tasks, move one to Done.
- Runs headless in CI; parallel to Newman.
- Minimal Selenium suite kept as a documented option.

## Acceptance Criteria

- [ ] Cypress core loop green in CI
- [ ] Copilot flow green with MSW stub (no LLM)


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Multi-tenant E2E journeys: onboard company (SuperAdmin) → admin console (Admin) → PM/Member flows → client feedback → handoff acceptance → maintenance request → offboard (backend 32–35 parity).
- Cross-tenant E2E negatives: org A users never see org B projects in any screen; suspended org renders the read-only state with working switch-org path.
- Client persona journey included end-to-end (progress view → feedback → handoff → maintenance) alongside the core loop.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
