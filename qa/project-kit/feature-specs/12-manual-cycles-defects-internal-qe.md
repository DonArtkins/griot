# Feature 12 - Full Manual Cycles + Defect Tracking + Internal QE (Week 7)

## Type

NEW FEATURE

## What This Delivers

The Week-7 deliverable block: execute full manual testing cycles against the deployed system, log & track every defect in Jira with evidence, and apply the same rigor to Sababisha's internal products as scheduled by the cohort.

## Dependencies

- Features 01-03 (cases + Jira). Features 06-11 (automated suites green).

## Context To Read First

- `research/week-07-real-world-qe-practice.md` sec 3

## Files Owned

- `docs/MANUAL-CYCLE-REPORT.md`, Jira project state

## Files

CREATE: `docs/MANUAL-CYCLE-REPORT.md` - per-cycle summary, defect list with severity/evidence, screenshots/videos.

## Implementation Notes

- Run against deployed Railway/Vercel, not localhost - env mismatch, CORS, cold-start are the targets.
- Traceability: every defect links to a test case + requirement.
- Internal-product QE is cohort-scheduled; the approach transfers.

## Acceptance Criteria

- [ ] Manual cycles executed and reported; all defects in Jira with evidence
- [ ] Traceability matrix maintained


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Multi-tenant manual cycles executed against the deployed Railway/Vercel with two seeded companies + the Client persona (org-switch, suspend, feedback, handoff paths).
- Defect taxonomy adds tenant-severity: a cross-tenant leak is Blocker regardless of affected scope; tenant defects trace to backend 29–35 requirements (qa 14 parity).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
