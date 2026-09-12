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
5. For API testing (specs 04–05): `backend/project-kit/feature-specs/20-observability-logging-pipeline.md` §routes (folder 14 trail assertions) and backend spec 18 (pagination/sort contract for strict assertions).

## Required Skills

Root shared skills + `qa/.agents/skills/` (all seven suites). Apply the relevant `SKILL.md` per feature.

## Where This System Sits in the Build Order (canonical: `docs/planning/IMPLEMENTATION-ROADMAP.md`)

**Phase P6 (Weeks 6–7)** — last: QA verifies the other six systems and needs the deployed topology for manual/UAT cycles. Specs 01–03 have zero code dependencies (gap fillers); 04's base collection exists (backend 08 ✅); 05 needs infra 05; 10–13 need everything deployed. Own order: **01 → … → 13**. Entry branch: `feature/qa/01-qe-fundamentals`. Track state in `qa/project-kit/context/progress-tracker.md`.

## Verification Gates (the product of this system)

- `dotnet test` (xUnit + WebApplicationFactory) green, incl. refresh-rotation replay + bulk atomicity.
- `npm test` (Jest+RTL) green; Flutter `flutter test` + integration green.
- Newman collection green in CI; Cypress core-loop green (stubbed copilot).
- k6 baseline + regression recorded; OWASP review logged; axe pass.
- Coverage >80% enforced (service-layer + auth emphasized).

## Hard Rules

1. The Postman collection (backend feature 08) is the contract suite - Newman reuses it; never a throwaway. **Folder 14 (`Audit — Logs & Trails`) ships tolerant assertions now; tighten to strict after backend 20/18/19/22 land (spec 20 §acceptance defines the strict set).**
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


## Multi-Tenant Migration Wave (2026-09-11 — PLANNED)

QA now proves **tenant isolation** as a first-class gate (canonical contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md`):

- **NEW spec 14 (PLANNED):** dedicated cross-tenant isolation suite — xUnit read/write rejection, IDOR/IDOT OWASP checks, Newman negative-org tests, k6 many-tenant scenario, client-boundary golden transcript assertions. Any cross-tenant leak = Blocker; isolation green gates the ship verdict (qa 13) and UAT (qa 12).
- **Coverage gate extended:** `IPermissionService` (org-role + permission resolution) joins the ≥80% service-layer/auth set (qa 11).
- **Specs 01–13** each carry a "Multi-Tenant Update (2026-09-11 — PLANNED)" section (multi-tenant manual/Jira matrix; org routes + JWT v2 claims + negative cross-tenant tests in Postman/Newman; isolation integration tests; org switcher + client-portal Jest tests; Flutter org-switch tests; multi-tenant Cypress E2E journeys; multi-tenant k6/OWASP; multi-tenant UAT + evidence).
- All of the above is PLANNED — no production code; implemented-status claims elsewhere in this file are unchanged until each spec ships on its own feature branch.

## Historical notes

These dated snapshots preserve prior decisions. Current work and verification are recorded in the owning progress tracker.

### Audit synchronization — 2026-09-11

Implemented through backend 20 (observability pipeline); backend 29 (multi-tenant foundation) implemented 2026-09-11 on `feature/backend/29-multi-tenant-foundation-organizations` — roadmap §P0.5 next is backend 30 after 29 completes its outstanding gates. Future planning is not completed implementation. P0 (2026-09-11): backend 29 ✅ → 30 → 31 → 32 → 33 → 34 → 35 → 18 → 19 → 22 → 21 → 23 → 11 → 28 → 24 → 25 → 26 → 27 → 10. P2: ai 01 → ai 02 → web 10 → ai 03 → ai 04 → ai 05 → ai 06 → ai 07 → web 11 → ai 08 → ai 09 → web 12 → ai 10 → ai 11 → ai 12. Full requirement/review ledger: `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`.
QA owns independent test evidence, not AI certification. Extend existing suites for template counts, >1,000 rows, phase eligibility, cross-role/source access, replay/idempotency, notices and human assignment approval.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
