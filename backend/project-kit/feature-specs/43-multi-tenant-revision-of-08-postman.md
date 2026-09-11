# Feature 43 — Multi-Tenant Revision of Feature 08 (API Testing — Postman) (own-stack)

## Type

NEW FEATURE · MULTI-TENANT MIGRATION WAVE (2026-09-11) · **PLANNED — not implemented** (revision of implemented spec 08; the original spec 08 file remains untouched — this revision supersedes it for tenant behavior)

## What This Delivers

The Postman collection updated for the tenant wave: org/role claims exercised in the auth folder, new folders for the **org/company/client/handoff** route families, and **negative cross-tenant tests** proving isolation at the API boundary — all Newman-green in CI.

## Dependencies

- Specs 39/42 (org-scoped REST + JWT v2 claims these tests assert)
- Specs 29/31/32/33/34/35 (org/role/client/handoff routes under test)
- Implemented spec 08 (collection structure, folder conventions, Newman CI job)

## Context To Read First

- `backend/Postman/Griot.postman_collection.json` (existing folders)
- `docs/api/auth-contract.md` (JWT v2 PLANNED claims)
- Original spec: `backend/project-kit/feature-specs/08-api-testing-postman.md`

## Agent Skills To Use

- Root `.agents/skills/contract-sync/SKILL.md`
- (Newman usage unchanged from spec 08 conventions)

## Files Owned

- `backend/Postman/Griot.postman_collection.json` (folders + environment + test scripts)
- `backend/Postman/README.md` (tenant test-matrix notes)
- Newman CI job update if a new folder changes the run order

## Implementation Notes

- **Auth folder** (01) updated: login response's access token is decoded (collection test script) and asserts `name`, `org`, `role`, `perms`; refresh token asserted **not** decodable (opaque 64-hex — per guide §4); `select-organization` success 200 (claims switch), non-member 403, unknown 404.
- **New folders (PLANNED)**: Organizations (SuperAdmin create/suspend/reactivate/transfer — specs 32/33), Roles (custom role CRUD + permission catalogue negatives — spec 31), Client Portal (invite → progress view → feedback → PM respond — spec 34), Handoff (checklist → documents → submit → accept → maintenance — spec 35), Tenant Lifecycle (export link, retention, purge negatives — spec 33).
- **Negative cross-tenant matrix** (the core addition): seeded two-org environment — user B (org 2) reads org 1's workspace/project/task/board → **404**; cross-org invite accept → 403/404; cross-org role CRUD → 403/404; cross-org idempotent-key reuse → rejected.
- **Suspend tests**: suspended org write → **403 `org_suspended`** (body + code asserted); reads/auth still 200.
- **Permission negatives**: Member without `task.manage`, Client attempting task create, PM without `org.roles.manage` → 403 each; response envelope stays `application/problem+json`.
- **Workspace-create completeness test**: `POST /api/workspaces` asserts `displayName`/`email`/`avatarUrl` present in the member payload, identical to `GET /api/workspaces` (locks the spec 39/46 fix).
- Client-view assertions: `/api/client/projects` responses asserted to contain **no** internal board internals (field allowlist test script).
- AI/OBO folder: service token + `X-On-Behalf-Of` asserted within one org; cross-org OBO → 403; raw-log reads stay closed.
- Strict assertion convention per spec 18/20 precedents (400s asserted as 400, envelope shape asserted) extended to every new folder; `GRIOT_RUN_SQL_TESTS=1` unaffected (Newman runs against the compose stack as today).

## Separation of Concerns

The collection stays the **machine-readable contract** consumed by spec 10's docs (its revision) and CI. Test data is collection-level fixtures only; no production seeds. Backend code changes belong to their owning specs — this spec only adds tests and asserts them green.

## Acceptance Criteria

- [ ] Auth folder asserts JWT v2 claims and the opaque refresh token (negative decode test)
- [ ] New org/company/client/handoff folders green in Newman (CI job updated)
- [ ] Cross-tenant negative matrix: every cross-org read/write returns 404/403 exactly as specified — no existence leak
- [ ] Suspended-org write → 403 `org_suspended`; read → 200 (asserted)
- [ ] `POST /api/workspaces` completeness + client-view field-allowlist assertions pass
- [ ] Newman green in CI; existing folders unbroken

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.