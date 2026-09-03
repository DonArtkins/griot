# QA Code Standards

- One suite = one concern (unit vs integration vs E2E vs contract vs perf). Never let one type crudely cover another's job.
- Tests live with their app: `backend/tests`, co-located `*.test.*` in web, `mobile/test`, `k6/`, `ai/`+`mcp/` vitest.
- Deterministic: no sleeps/timing flakes; use waits/retries; MSW stubs for the copilot; mocked LLM in ai tests.
- Nominal + adversarial cases; assert behavior, minimal snapshots.
- Log every finding in Jira with evidence; traceability requires requirement <-> test <-> defect.
