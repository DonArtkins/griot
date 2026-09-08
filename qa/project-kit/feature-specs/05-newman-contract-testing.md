# Feature 05 - Newman CLI Automation + Contract Testing

## Type

NEW FEATURE

## What This Delivers

The Week-6 step 4/5 deliverable: Newman CLI runs of the collection in CI with JUnit reporting, and contract testing via JSON schema assertions on core endpoints.

## Dependencies

- Feature 04 (extended collection).
- Infra feature 05 (CI jobs).

## Context To Read First

- `research/week-06-quality-engineering-foundations.md` sec 4

## Agent Skills To Use

- `qa/.agents/skills/newman-api/SKILL.md`

## Files Owned

- `.github/workflows/ci-cd.yml` (newman job) - owned jointly with infra
- `k6/` no; `docs/NEWMAN-NOTES.md`

## Files

CREATE: `docs/NEWMAN-NOTES.md` - runbook + expected thresholds.
MODIFY: CI - newman job `newman run ... --reporters junit` parallel to Cypress.

## Implementation Notes

- Contract checks: `pm.response.to.have.jsonSchema` on auth, task, board, dashboard responses.
- Runs on every PR and weekly against the deployed API.

## Acceptance Criteria

- [ ] Newman green in CI; JUnit artifact uploaded
- [ ] Core contracts schema-asserted


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
