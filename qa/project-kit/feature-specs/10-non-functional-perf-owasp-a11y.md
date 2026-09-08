# Feature 10 - Non-Functional: Performance (k6) + OWASP + Accessibility

## Type

NEW FEATURE

## What This Delivers

The Week-6 step 7 deliverable: k6 load baselines (dashboard query, login, board read), the OWASP Top-10 review on the owned auth surface, and accessibility (axe) checks on both shells.

## Dependencies

- Deployed API (Railway) for realistic baselines.
- Backend + web features complete.

## Context To Read First

- `research/week-06-quality-engineering-foundations.md` sec 7
- `qa/project-kit/context/environments.md`

## Agent Skills To Use

- `qa/.agents/skills/k6-perf/SKILL.md`, `qa/.agents/skills/owasp-review/SKILL.md`

## Files Owned

- `k6/**`, `docs/PERF-BASELINE.md`, `docs/OWASP-REVIEW.md`, `docs/A11Y-NOTES.md`

## Files

CREATE: k6 scripts + baseline report; OWASP checklist entries per item; axe runs + findings.

## Implementation Notes

- OWASP items: injection (parameterized SQL), broken auth (rotation replay), sensitive data (no localStorage), CORS, AI principal scope (prompt injection = data not instructions).
- a11y: axe via RTL and Lighthouse; WCAG AA.

## Acceptance Criteria

- [ ] k6 baseline recorded (p95 < 500ms targets)
- [ ] OWASP review logged per item (finding or no-issue)
- [ ] axe pass on both shells


## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
