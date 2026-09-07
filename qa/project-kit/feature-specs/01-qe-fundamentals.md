# Feature 01 - QE Fundamentals (SDLC/STLC, Testing Types, KPIs)

## Type

NEW FEATURE

## What This Delivers

The Week-6 step 1 deliverable: a structured brief of SDLC vs STLC, the testing types Griot uses (unit, integration, system, regression, UAT), and the KPI set tracked through Week 7.

## Dependencies

- None (documentation-first).

## Context To Read First

- `research/week-06-quality-engineering-foundations.md` sec 1
- `qa/project-kit/context/{test-pyramid,coverage-gate}.md`

## Files Owned

- `docs/QE-FUNDAMENTALS.md`

## Files

CREATE: `docs/QE-FUNDAMENTALS.md` - map each testing type to a Griot surface/suite; KPI table (pass rate, defect leak, coverage, p95, defect density) with targets.

## Implementation Notes

- KPIs feed the Week-7 exec summary; record definitions now so numbers are comparable.

## Separation of Concerns

- This is a documentation feature; it defines vocabulary, not tests.

## Docker & Deploy

- N/A (doc).

## Acceptance Criteria

- [ ] Each testing type maps to a named Griot suite
- [ ] KPI definitions + targets recorded


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
