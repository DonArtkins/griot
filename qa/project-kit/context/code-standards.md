# QA Code Standards

- One suite = one concern (unit vs integration vs E2E vs contract vs perf). Never let one type crudely cover another's job.
- Tests live with their app: `backend/tests`, co-located `*.test.*` in web, `mobile/test`, `k6/`, `ai/`+`mcp/` vitest.
- Deterministic: no sleeps/timing flakes; use waits/retries; MSW stubs for the copilot; mocked LLM in ai tests.
- Nominal + adversarial cases; assert behavior, minimal snapshots.
- Log every finding in Jira with evidence; traceability requires requirement <-> test <-> defect.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
