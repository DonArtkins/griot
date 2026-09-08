# Feature 03 - Test Management in Jira

## Type

NEW FEATURE

## What This Delivers

The Week-6 step 3 deliverable: the Jira workflow for tests, test cycles, and defects, plus the traceability matrix (requirement <-> test <-> defect).

## Dependencies

- Features 01-02 (cases + taxonomy).

## Context To Read First

- `research/week-06-quality-engineering-foundations.md` sec 3

## Files Owned

- `docs/JIRA-WORKFLOW.md`, `docs/TRACEABILITY-MATRIX.md`

## Files

CREATE: `docs/JIRA-WORKFLOW.md` - projects, issue types (Story/Bug/Test), linking, sprint flow, test-cycle conventions.
CREATE: `docs/TRACEABILITY-MATRIX.md` - template mapping requirement <-> test case <-> defect id.

## Implementation Notes

- Seed the Jira project layout now; Week-7 evidence attaches to these ids.

## Acceptance Criteria

- [ ] Jira workflow documented and usable
- [ ] Traceability matrix template in place


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
