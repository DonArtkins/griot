# Backend Feature Spec 21 — Database Resilience: Triggers, Backups & Restore Readiness

**Status:** PLANNED — the 2026-09-10 audit found **zero `CREATE TRIGGER` objects** and **no automated backup** of the SQL Server database anywhere in the repo (verified by repo-wide search). PITR exists only as a Railway *PostgreSQL* runbook (`docs/planning/RUNBOOK-ROLLBACK.md`); nothing protects the primary SQL Server store.

## What This Delivers

1. **DB-level audit triggers** — a safety net that records row changes even if application code (spec 20) is bypassed by raw SQL, a migration bug, or a future contributor.
2. **A backup chain with a rehearsed restore** — copies of the database that a crash, a bad deploy, or an attack (ransomware-style wipe) cannot destroy, plus load-balancing/scale-out posture so the backend stays fast under read pressure.

## Dependencies

- Feature 02 (✅ schema), Feature 20 (writes `AuditLogs` — triggers write the same table).

## Files Owned

- `backend/src/Griot.Infrastructure/Sql/audit-triggers.sql` (idempotent)
- `backend/src/Griot.Infrastructure/Sql/usp_PruneObservabilityLogs.sql` (shared with spec 20)
- `infra/` compose additions (backup sidecar — see infra spec 03 sync note)
- `docs/database/BACKUP-RESTORE-DRILL.md` (new runbook, linked from RUNBOOK-ROLLBACK)

## A. Audit triggers (planned)

`AFTER INSERT/UPDATE/DELETE` triggers on **operational tables only** — `TaskItems`, `WorkspaceMembers`, `Invites`, `Attachments` — each writing `AuditLogs` rows with `Action = 'DB.<table>.<INS|UPD|DEL>'`, `ActorId` best-effort (app-set `SESSION_CONTEXT(N'ActorId')`; all-zeros GUID when absent, e.g. migrations), `Before`/`After` from `deleted`/`inserted` pseudo-tables as JSON.

- **Idempotent creation** (`IF OBJECT_ID(...) IS NULL EXEC(...)`), applied by the same sqlcmd release step as specs 03/20.
- **Guard rails:** no triggers on `ApiLogs`/`ErrorLogs`/`AuditLogs` themselves (no recursion/amplification); none on `RefreshTokens`/`OtpChallenges` (secrets adjacent — spec 20 §6 owns their audit); trigger failure rolls back the statement (SQL Server semantics) and is treated as a P2 incident via `ErrorLogs` correlation.
- Application writes (spec 20 `IAuditService`) remain the **primary** audit path with real actor/`ActivityId` linkage; triggers are the lower-fidelity catch-all. Dedupe convention: app rows carry `Action` without the `DB.` prefix.

## B. Backups & restore (planned)

| Piece | Design |
|---|---|
| Recovery model | `FULL` (required for log backups) — set once, idempotent script |
| Full backup | nightly 02:00 UTC → volume `sababisha_mssql_backup` |
| Differential | every 15 min |
| Log backup | every 10 min (point-in-time to last log backup) |
| Executor | opt-in compose **sidecar container** (`profile: backup`, `mcr.microsoft.com/mssql/tools`) running the backup T-SQL on schedule — dev parity for what Railway does via managed snapshots in prod |
| Retention | 7 daily, 4 weekly locally; Railway volume snapshots per `docs/planning/CAPACITY-PLAN.md` |
| Off-volume copy | weekly copy to object storage explicitly out of scope v1 — Phase 3 with cost gate |
| Restore drill | `RESTORE ... WITH VERIFYONLY` + full restore-to-scratch-DB rehearsal **once per bootcamp phase**, logged in `BACKUP-RESTORE-DRILL.md` |
| Incident flow | crash/attack → isolate → restore latest FULL + DIFF + LOGs to a scratch DB → validate (`AuditLogs`/`ActivityLogs` timestamp smoke per RUNBOOK-ROLLBACK) → switch `ConnectionStrings__Default` → redeploy |

## C. Load balancing / scale-out posture (planned, evidence-gated)

- **Now (v1):** single SQL Server primary + Redis cache (spec 19) + capped queries (spec 18) + indexes per `DATABASE-DESIGN.md`. The stateless API container already scales horizontally behind Railway's HTTP load balancing; DB read pressure is what caching + pagination manage.
- **Phase 2 (k6-gated, unchanged decision):** readable secondary / EF read-only context routing — `DATABASE-DESIGN.md` §5 stays the contract; this spec adds only the **connection-string plumbing test** (read/write split config keys exist but point to the same server until evidence demands otherwise).

## Separation of Concerns

Trigger + backup SQL lives in `Griot.Infrastructure/Sql/` (data layer); the sidecar + schedule is `infra/` (no app code); runbooks live in `docs/`.

## Docker & Deploy

- Compose gains the backup sidecar (infra spec 03 sync): volume `sababisha_mssql_backup`, profile `backup` (off by default locally; enabled in the nightly/manual run).
- Railway prod: volume snapshots + the same T-SQL backup job via release command schedule (infra spec 06 note).

## Out of Scope

Always-On AG / failover clusters, geo-replication, log shipping to a warm standby (post-bootcamp; cost-gated).

## Tri-agent verification note (2026-09-10)

Independent audits agree on the gap this spec closes: the repo audit
(`docs/observability/LOGGING-AUDIT-REPORT.md`, findings F5/F6) found **zero CREATE
TRIGGER objects** and **no SQL Server backup chain**, and ADR-004 records the decision.
A separate infrastructure review of the same branch independently recommended:
(a) T-SQL DML audit triggers as the DB-level safety net, (b) nightly FULL + frequent
DIFF/LOG backups with documented restore drills, (c) a load-balancing posture review
(read replicas stay Phase-2, k6-evidence-gated), (d) test coverage for
pagination/filter limits. All four are covered by this spec + specs 18/19/20 —
redundant implementation MUST NOT be started outside the spec + hard gates.

## Acceptance Criteria (all pending)

- [ ] `audit-triggers.sql` re-runnable; triggers exist on the four tables only
- [ ] Raw SQL `UPDATE TaskItems SET ...` outside the app produces an `AuditLogs` row with `DB.`-prefixed action (integration test)
- [ ] Backup chain produces FULL+DIFF+LOG files on the backup volume; timestamps verify
- [ ] Restore drill executed once and documented (scratch DB passes the AuditLogs timestamp smoke)
- [ ] Prune proc (spec 20) does not fight triggers (no AuditLogs recursion)
- [ ] `.env.example` + compose carry the backup sidecar rows; infra specs 03/06 sync notes merged

## Multi-Tenant Update (2026-09-11 — PLANNED)

- **Triggers become org-stamped:** `audit-triggers.sql` writes `AuditLogs` rows carrying the affected row's `OrganizationId` (best-effort `SESSION_CONTEXT(N'ActorOrgId')` alongside `N'ActorId'`; NULL for migration/platform paths) — the DB-level safety net answers "which tenant" too.
- **Backup/restore drills include a per-org export restore check:** after `RESTORE ... WITH VERIFYONLY` + the scratch-DB rehearsal, the drill spot-checks one seeded org's rows by `OrganizationId` (row counts + `AuditLogs`/`ActivityLogs` timestamp smoke) before any cutover per RUNBOOK-ROLLBACK.
- **Offboarding purge interplay (spec 33):** purge runs as org-scoped chunked bulk deletes (spec 41 revision) — trigger behavior during purge is verified (no unbounded `AuditLogs` amplification, no recursion on the purge's own audit rows); the purge path is part of the trigger guard-rail tests.
- Trigger coverage of the **new tenant tables** (`OrganizationMembers`, `Roles`, `OrganizationInvites`, `ProjectClients`, `ClientFeedback`) is added to the operational-table list once spec 37's migration ships.
- Restore runbook gains the tenant note: the PITR-style cutover (`docs/planning/RUNBOOK-ROLLBACK.md`) validates `AuditLogs`/`ActivityLogs` timestamps **per OrganizationId**, so a tenant-scoped rollback window is provable, not inferred.
- Prune proc (spec 20/51 revision) carries the new nullable `OrganizationId` through untouched — prune/trigger non-fighting acceptance still holds.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.