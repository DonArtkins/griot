# QA Feature Spec 14 — Multi-Tenant Isolation Tests

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented**

## What This Delivers

A dedicated cross-tenant isolation suite that proves **no cross-tenant leak in any surface** (canonical contract: `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` §1): xUnit cross-tenant read/write rejection against two seeded companies, IDOR/IDOT OWASP checks, Postman/Newman negative org tests, a k6 many-tenant scenario, and client-boundary golden transcript assertions. Any cross-tenant leak = **Blocker** and gates the ship verdict (qa 13).

## Dependencies

- Backend 29 (tenancy foundation + EF global filters), 30 (JWT v2 claims), 32/33 (lifecycle), 34/35 (client portal/handoff), 51 (org-stamped logs).
- QA 04–06 (Postman/Newman/xUnit suites exist), QA 10 (k6 + OWASP pattern), infra 05 (CI jobs).

## Context To Read First

- `docs/multi-tenancy/MULTI-TENANCY-GUIDE.md` (§1 isolation layers, §3 roles, §4 JWT v2, §5 lifecycle)
- `qa/project-kit/context/{test-pyramid,coverage-gate,environments}.md`
- `docs/api/auth-contract.md` (PLANNED multi-tenant section) + backend spec 25 (role tiers)

## Agent Skills To Use

- `qa/.agents/skills/xunit-dotnet/SKILL.md`, `qa/.agents/skills/newman-api/SKILL.md`
- `qa/.agents/skills/k6-perf/SKILL.md`, `qa/.agents/skills/owasp-review/SKILL.md`
- `.agents/skills/jwt-decode/SKILL.md` (assert `org`/`role`/`perms` claim shapes)

## Files Owned

- `backend/tests/Griot.Tests/**` isolation suites (owned jointly with the backend system)
- `Postman/Griot.postman_collection.json` (isolation/negative-org folder), `k6/multi-tenant.js`
- `docs/TENANT-ISOLATION-EVIDENCE.md`

## Implementation Notes

- xUnit integration tests boot `WebApplicationFactory<Program>` with two seeded orgs + one user per role tier: cross-tenant reads return **empty**, writes are **rejected** (403 / empty-result for reads), EF global filters + repository guards proven — including org A ids in URLs, bodies and GraphQL arguments.
- Newman negative-org folder: schema-asserted 403/empty responses across org boundaries; JWT v2 claim assertions (`name`/`org`/`role`/`perms` present; refresh token body opaque — never a JWT); suspended-org write rejection (`403 org_suspended`).
- k6 many-tenant scenario: constant-arrival load with per-org tags; p95 asserted **per tenant** within the `docs/planning/NFR.md` targets.
- Client-boundary golden transcript assertions: a `Client`-tier session never receives internal board internals, raw logs, other tenants' data or internal capability names — asserted at the tool-call level, not in prose.
- Coverage gate extended: `IPermissionService` (org-role + permission resolution, custom `custom:{roleId}`) counts toward the ≥80% service-layer/auth set.

## Separation of Concerns

- QA owns the evidence; backend owns the isolation code under test; CI (infra 05) only runs the suites — no QA code inside backend logic.

## Docker & Deploy

- Runs in CI service containers (SQL Server 2022) per infra 05; manual runs against the deployed Railway API for the OWASP/k6 evidence, never only localhost.

## Out of Scope

- Performance regression vs the Week-6 baseline (qa 13) and full manual/UAT cycles (qa 12–13) — this spec owns isolation only.
- Backend isolation code itself (backend 29–35 specs own it).

## Acceptance Criteria

- [ ] xUnit isolation suites green: cross-tenant reads empty, writes rejected, no cross-org leak in any repository/query path; `IPermissionService` ≥80% coverage
- [ ] Postman/Newman negative-org suite green in CI: 403/empty schema-asserted across org boundaries; JWT v2 claims asserted; `org_suspended` writes rejected
- [ ] OWASP IDOR/IDOT checks logged per item (finding or no-issue) with org-boundary evidence
- [ ] k6 many-tenant scenario recorded; per-org p95 within targets
- [ ] Client-boundary golden transcript assertions green: Client tier never receives internal board internals or other tenants' data
- [ ] `docs/TENANT-ISOLATION-EVIDENCE.md` records the full isolation pass (any leak = Blocker, not-ready verdict)

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.