# Feature 11 - DevOps Integration & TDD (>80% Coverage Gate)

## Type

NEW FEATURE (MODIFICATION - hardens CI gates from infra feature 05)

## What This Delivers

The Week-6 step 8 deliverable: TDD discipline and the enforcement of the >80% coverage gate across all suites in CI.

## Dependencies

- Infra feature 05 (CI exists). Features 06-10 (suites exist).

## Context To Read First

- `qa/project-kit/context/{coverage-gate,environments}.md`

## Files Owned

- workflow job `test-dotnet`/`test-web`/`test-mobile` coverage steps (with infra)
- `docs/COVERAGE-GATE.md`

## Files

MODIFY: CI - coverage collection (`XPlat`, jest/lcov, flutter lcov) + threshold enforcement (80% service-layer/auth; aggregate check).
CREATE: `docs/COVERAGE-GATE.md` - thresholds, weighting rule, and how to read the reports.

## Implementation Notes

- Service-layer + auth first; presentation second; flat-aggregate pass while auth uncovered = failure.

## Acceptance Criteria

- [ ] CI blocks merges below threshold
- [ ] Coverage report artifact uploaded each run


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
