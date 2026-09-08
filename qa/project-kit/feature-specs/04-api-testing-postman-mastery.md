# Feature 04 - API Testing Mastery (Advanced Postman Collections)

## Type

NEW FEATURE

## What This Delivers

The Week-6 step 4 deliverable: advanced Postman collections - environment variables, assertions, reusable scripts, and GraphQL coverage on top of the backend feature-07 collection.

## Dependencies

- Backend feature 07 (base collection).

## Context To Read First

- `research/week-06-quality-engineering-foundations.md` sec 4

## Agent Skills To Use

- `qa/.agents/skills/newman-api/SKILL.md`

## Files Owned

- `Postman/Griot.postman_collection.json` (extended)

## Files

MODIFY: collection - add pre-request/assertion scripts, negative cases (401/403/404/429), GraphQL query/mutation steps with schema assertions, chained refresh.

## Implementation Notes

- Assertions cover response time, status, JSON schema for key contracts.
- Keep the collection runnable headlessly (Newman) - this is the contract suite.

## Acceptance Criteria

- [ ] Negative + positive API cases present; env chaining works
- [ ] Collection green for REST + GraphQL


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
