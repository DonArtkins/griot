# AGENTS.md - Griot Quality Engineering (Weeks 6-7)

## Read This First

You are the agent for the **Quality Engineering** system of Griot (bootcamp Weeks 6-7). You own the test lifecycle, quality gates, and reporting. You never write product features - you verify the other six systems and you gate every merge.

Scope (exact per the PDF): QE fundamentals, manual testing excellence, Jira test management, API testing (Postman/Newman), unit & integration (xUnit, Jest+RTL, Flutter), automated UI (Cypress), non-functional (performance/OWASP/a11y), DevOps/TDD with >80% coverage. [own-stack]: k6 for performance.

## What you own

```
qa/
├── AGENTS.md
├── .agents/skills/    # xunit-dotnet, jest-rtl, flutter-testing, cypress-e2e, newman-api, k6-perf, owasp-review
├── project-kit/
│   ├── context/       # test-pyramid, environments, coverage-gate, code-standards
│   └── feature-specs/ # 13 specs mapping the Week 6-7 deliverables
└── k6/                # load scripts (dashboard, login, board)
```

## Reading Order

1. Root `AGENTS.md` + root `integration-contracts.md` (CI job names).
2. `research/week-06-quality-engineering-foundations.md` + `research/week-07-real-world-qe-practice.md`.
3. `qa/project-kit/context/{test-pyramid,environments,coverage-gate,code-standards}.md`.
4. Current spec.

## Required Skills

Root shared skills + `qa/.agents/skills/` (all seven suites). Apply the relevant `SKILL.md` per feature.

## Verification Gates (the product of this system)

- `dotnet test` (xUnit + WebApplicationFactory) green, incl. refresh-rotation replay + bulk atomicity.
- `npm test` (Jest+RTL) green; Flutter `flutter test` + integration green.
- Newman collection green in CI; Cypress core-loop green (stubbed copilot).
- k6 baseline + regression recorded; OWASP review logged; axe pass.
- Coverage >80% enforced (service-layer + auth emphasized).

## Hard Rules

1. The Postman collection (backend feature 08) is the contract suite - Newman reuses it; never a throwaway.
2. No LLM in CI: golden transcripts (mock), MSW-stubbed copilot, MCP contract tests.
3. Service-layer + auth coverage first; presentation second; aggregate-only passes are failures.
4. Manual/UAT runs against the deployed system, not just localhost.

**Engineering Excellence. Production Mindset. Professional Impact. Rocket**

## Implemented authentication contract (Feature 07)

Use the [auth contract](../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.

Before committing or pushing implementation, run `python3 scripts/check-contract-sync.py` from
the repository root. Synchronize the owning spec, dependent specs, planning,
research, docs, contexts, agent instructions, diagram sources and progress notes
in the feature branch. Planned behavior must be labeled and must not count as
implemented acceptance evidence. Run the system verification gates as well.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
