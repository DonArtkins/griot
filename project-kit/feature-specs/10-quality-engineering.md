# Feature 10 — Quality Engineering: The Week-6/7 Gate

## Type

NEW FEATURE (MODIFICATION of every prior feature's test surface)

## What This Delivers

The complete bootcamp quality chain materialized: xUnit unit + integration suites (incl. the refresh-rotation replay race), Jest + RTL, Flutter widget/integration, Cypress E2E, Newman API contract, k6 performance baselines, OWASP security review, accessibility (axe) gate, ≥80% coverage enforcement, and the Week-7 manual/UAT reporting artifacts. Every suite runs in the Feature-08 CI.

## Dependencies

- Features 04–07 (each app's test harness exists from its first feature — this spec *completes and hardens* them).
- Feature 08 (CI jobs run these suites and gate merges).

## Context To Read First

- `context/test-validation-plan.md`
- `research/week-06-quality-engineering-foundations.md`
- `research/week-07-real-world-qe-practice.md`

## Files Owned

- `backend/tests/**` (xUnit)
- `web/src/**/*.test.{ts,tsx}` + Cypress config/tests (`web/cypress/**`)
- `mobile/test/**`, `mobile/integration_test/**`
- `k6/**` load scripts
- `docs/OWASP-REVIEW.md`, `docs/TEST-STRATEGY.md`, `docs/EXECUTIVE-TEST-SUMMARY.md`
- `AI_REVIEW findings` (from CodeRabbit or equivalent, applied before merge)

## Files

CREATE: `backend/tests/Griot.Tests/` — `AuthApiTests` (refresh replay, rate limit, scope), `TaskServiceTests` (`BulkUpdate_Rotates_Statuses_Atomically`), per-module service tests; coverage threshold config.
CREATE: `web/cypress/` — core-loop E2E (signup → project → tasks → drag to Done) + copilot flows with MSW stub.
CREATE: `k6/` — `dashboard-load.js`, `login.js`, `board.js`; thresholds p95 < 500 ms.
CREATE: `docs/OWASP-REVIEW.md` — per-item checklist (injection, broken auth, sensitive data, CORS, AI principal).
CREATE: `docs/TEST-STRATEGY.md` — living strategy (started Week 2, finalized Week 7) per `week-07` §2 outline.
CREATE: `docs/EXECUTIVE-TEST-SUMMARY.md` — Week-7 output: verdict, pass/fail by area, top-3 risks, recommendation.
MODIFY: CI workflow — coverage gate (≥80% service-layer/auth), Newman `--reporters junit`, k6 regression job, axe/Lighthouse job.

## Setup / Initialization

```bash
# Backend tests
cd backend
dotnet new xunit -o tests/Griot.Tests && dotnet add tests/Griot.Tests reference src/Griot.Api
dotnet add tests/Griot.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet test --collect:"XPlat Code Coverage"

# Web E2E + unit
cd web
npm i -D cypress @testing-library/cypress
npx cypress open                # then record run in CI headlessly

# Performance
cd k6 && npm init -y && npm i -D typescript
k6 run dashboard-load.js -e API_URL=… -e TOKEN=…
```

## Separation of Concerns

- Test type ↔ layer: unit tests mock collaborators; integration tests use `WebApplicationFactory` + real containers; E2E drives real UI against the deployed API; contract tests reuse the Postman collection — never one test type crudely covering another's job.
- Each app's tests live beside/appended to that app (`backend/tests`, `web/cypress`, `mobile/test`, `k6/`).
- The AI layer is tested without any LLM network: golden transcripts (mocked client) + MCP pure-function contracts + MSW-stubbed copilot in Cypress.

## Docker & Deploy

- CI service containers (SQL Server 2022, Postgres, Redis as needed) provide hermetic test environments — no developer-local state required.
- Newman + Cypress run **against the deployed Railway/Vercel system** at least weekly (Week-7 manual piggybacks on the same URLs).
- Coverage artifacts uploaded to CI; gates block `main` when thresholds are missed.

## Out of Scope

- Property-based testing frameworks, fuzzing pipelines, third-party pen-test engagement.

## Acceptance Criteria

- [ ] All suites green in CI: xUnit, Jest+RTL, Flutter, Cypress, Newman
- [ ] Refresh-rotation replay + bulk-update atomicity + N+1 checks explicitly covered
- [ ] Coverage ≥80% enforced (service-layer/auth emphasized)
- [ ] k6 baseline + regression recorded against deployed API
- [ ] OWASP review logged per item; axe pass on Public shell
- [ ] Week-7: manual cycle + UAT + executive summary completed; all defects tracked in Jira
- [ ] Test strategy doc finalized from the living draft

## Future Modifications

- Post-bootcamp: pipeline hardening (SBOM, container scanning), real-device farm smoke tests.