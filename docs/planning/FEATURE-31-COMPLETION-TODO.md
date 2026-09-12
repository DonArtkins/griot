# Feature 31 completion and documentation repair

Branch: `feature/backend/31-rbac-roles-custom-permissions`.
Structure reference: commit `1f3223b9f8dfd5edb0f2afd2a32212145e44eb1a`.

## TODO — ALL COMPLETE (2026-09-12)

- [x] Audit tracked Markdown and agent files against the reference structure.
- [x] Repair misplaced history, duplicate footers, broken tables, and stale current-state instructions while retaining current contracts.
- [x] Restore all progress trackers with accurate state, spec coverage, next steps, and contained session notes.
- [x] Audit feature 31 implementation and tests against every acceptance criterion.
- [x] Complete missing RBAC behavior and meaningful regression coverage.
- [x] Synchronize feature 31 API, dependent specs, contexts, agent instructions, and changelog.
- [x] Verify Markdown structure, links, and contract synchronization.
- [x] Run backend build, unit/integration tests, SQL gates, and local health checks.
- [x] Review the final diff and record evidence and any remaining limitations.

## Scope

The reference commit supplies document organization; later approved tenancy and
authentication contracts remain current. Session history belongs under Session
Notes in trackers or a dedicated historical section in other documents. Feature
32 and later implementations remain planned.

## Verification (final, 2026-09-12)

- `dotnet build`: 0 warnings / 0 errors.
- Full test suite with `GRIOT_RUN_SQL_TESTS=1` (live SQL Server container): **212 passed / 0 skipped / 0 failed**, including the 5 `RoleSqlTests` HTTP-integration tests and the prune stored-procedure test.
- Two test failures found and fixed during the final run (both test fixtures, no production change):
  `RoleSelection_SuperAdminWins_AndCustomRoleFormats` (fixture now sets `CustomRole.OrganizationId` to satisfy the cross-tenant invariant) and
  `ObservabilityPruneSqlTests` (now seeds a real `Organizations` row before the workspace — spec-29 FK on `Workspaces.OrganizationId`).
- `/health` on the locally booted API: HTTP 200 `Healthy`.
- `python3 scripts/check-contract-sync.py`: exit 0 (703 files inspected).
- Spec 31 acceptance criteria: all 7 checked with named test evidence in
  `backend/project-kit/feature-specs/31-rbac-roles-custom-permissions.md`.
- Remaining limitation (by design, not a defect): spec 20's durable outbox /
  webhook inbox / idempotency store stay PLANNED until Trigger.dev callers ship;
  valid-HMAC callbacks still return 503.
