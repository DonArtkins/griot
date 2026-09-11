# Backend 29 preflight and completion plan

Date: 2026-09-11. Branch: `feature/backend/29-multi-tenant-foundation-organizations`.
Status (2026-09-11, superseded by implementation): this preflight snapshot described the
working tree before the spec-29 implementation commit `9447e15` — the feature is now
**✅ COMPLETE**: all gates closed 2026-09-11 (tenancy ERD approved by the operator and
exported as `diagrams/erd/griot-erd-v2.0.0.png` + `griot-erd2-v2.0.0.png`; the
migration-apply gate closed with the rebuilt database verified up to date). The findings
below are preserved as history.

## Outcome

The requested commit/push and transition to another feature were not performed.
The working tree already contained tenancy production code, a migration, and separate
spec-23/account-deletion planning edits when this run began. Those changes were preserved.
This run changed documentation only. No database migration, deployment, destructive
command, or sudo command was run. No feature is marked complete by this report.

The roadmap's P0.5 sequence governs: finish backend 29, then backend 30 after
feature completion, push, and explicit user approval. Older instructions naming
backend 18 or 20 as next are superseded by that sequence. The backend tracker records
spec 20's logging writers as delivered; its durable dispatch work remains planned.

## Verification evidence

| Check | Result |
|---|---|
| `dotnet build --no-restore --nologo -m:1` in backend | Passed; zero warnings/errors |
| `dotnet test --no-build --no-restore --nologo -m:1` with Brevo API keys cleared | 152 passed, 8 SQL tests skipped, 0 failed |
| Test artifact | `/tmp/griot-spec29-preflight/spec29-preflight.trx` (local, not committed) |
| Root `python3 scripts/check-contract-sync.py` | Passed after documentation edits: 683 files inspected, 248 branch changes |
| `git diff --check` | Passed after documentation edits |
| Docker inspection | SQL Server, PostgreSQL and Redis containers running on ports 14333, 5433 and 6380; application health and database connectivity not established by this check |
| Root Husky | `core.hooksPath=.husky/_`; tracked hooks exist only at root; pre-commit runs contract sync, pre-push runs build/tests/contract sync |
| Commit/push hooks | Not executed through Git because commit/push readiness is not established; no hook bypass used |

The first test attempt aborted because the sandbox denied the test runner's local
socket. The same command succeeded with tool-approved escalation. This was an
environment restriction, not a code defect. SQL tests were not enabled: their fixture
applies all migrations, including the still-unapproved tenancy amendment, to a
disposable database. The passing mock/unit tests do not establish SQL isolation or
legacy-data migration acceptance.

## Outstanding findings

These are source-review findings, not claims of reproduced production incidents.

1. **Design approval is missing from the repository.**
   `diagrams/erd/multi-tenant-amendment.md` explicitly remains pending Figma Make
   approval; the diagram ledger has no approved tenancy export. Spec 29's acceptance
   boxes are unchecked. Root AGENTS rules 4/5 and the ERD skill require approval first.
2. **Schema differs from its planned contract.** Code uses `PlanName` instead of
   `Plan`, makes `ActivityLog.OrganizationId` non-nullable, and uses `InviteStatus`
   for organization membership while migration backfill writes `Active`.
   Planned membership states are `Invited/Active/Suspended`. Named indexes and
   workspace-to-organization delete behavior also differ. Resolve these in the
   design before changing schema code; do not relabel the current migration approved.
3. **Backfill needs relational validation.** Required tenant columns are added
   without a staged nullable/backfill/required sequence. Owner membership insertion
   selects workspace rows without deduplicating owners who own multiple workspaces.
   Quarantine ownership uses an unordered user selection, owner slugs truncate GUIDs,
   and `PlatformRole` defaults to an empty string. Validate empty and populated
   historical databases and agree deterministic ownership for ambiguous records.
4. **Tenant enforcement is incomplete.** `DomainService` stamps workspace creation
   but other new tenant entities are not consistently stamped. `SaveChanges` only
   updates timestamps. `Organizations` and `WorkspaceMembers` have no tenant filter;
   log filters allow every organization when the context tenant is null. The planned
   explicit, authorized and audited platform bypass is not implemented.
5. **Factory consumers do not share tenant initialization.** The scoped registration
   calls `WithTenant`, while GraphQL DataLoaders create contexts directly from the
   pooled factory. Prove isolation when contexts are reused, including concurrent
   requests, absent claims, background telemetry and AI OBO requests.
6. **Acceptance coverage and read surface are missing.** The new tenancy tests use
   mocks; they do not prove SQL filtering, pooled-context isolation or migration
   preservation. Spec 29 names organization REST/GraphQL reads but does not define
   exact routes or response shapes. Current JWT issuance has no spec-30 `org` claim;
   document the fail-closed interim behavior without implementing spec 30 early.
7. **Commit scope and semantic synchronization remain unresolved.** Existing spec-23
   planning edits and untracked agent/skill-install artifacts need review and their
   proper feature branches. The historical backend-09 grouping exception does not
   authorize a new multi-feature batch. A successful sync script does not validate
   the schema, runtime isolation, or all prose contracts.

## Proposed completion plan — approval required

1. Finish the spec-29 reading checklist, including referenced backend contracts,
   relevant dependent revision specs, design prompts and diagrams. This preflight
   did not complete the user's requested recursive reading of every system's files.
   Use Context7 and the system skills before implementation that depends on current APIs.
2. Reconcile the foundation subset of the ERD with the canonical guide: exact field
   and enum names, nullable log tenancy, index names, FK behavior, deterministic
   legacy ownership, and membership backfill. Complete the organization read API
   design and document the spec-29/spec-30 transition. Preserve the one-spec boundary.
3. Prepare the Figma Make amendment and versioned export, obtain human approval,
   and record it in `diagrams/README.md`. Obtain approval of the concrete implementation
   plan before production changes, as required by root AGENTS rule 5.
4. Repair spec 29 on its existing branch: tenant resolution, all factory consumers,
   per-entity reads/writes and parent ownership validation, authorized/audited platform
   access, organization reads, and the workspace POST member-data fix. Review Dapper
   paths against the same tenant boundary; keep their implementation within owning
   spec/revision boundaries. Do not add JWT v2 issuance or company lifecycle endpoints.
5. Correct the unapplied migration only after checking actual migration history.
   Use staged backfill and constraints; test existing data and multiple workspaces
   per owner. Do not reset, drop, truncate, or migrate the operator's Griot database
   as a substitute for a preservation test.
6. Run relational isolation, concurrent/pool reuse, write rejection, migration and
   REST/GraphQL parity tests; verify Docker dependencies and API health; update
   Postman coverage. Synchronize the owning/dependent specs, contexts, agent files,
   diagram ledger, research references, documentation and trackers with actual results.
7. Review and separate unrelated existing edits without discarding them. Once all
   gates pass, update the tracker, commit and push this feature through root Husky,
   issue the completion report, and wait for explicit approval before backend 30.

## Operator steps now

No installation, Docker restart or migration is required for this preflight.
The current tenancy migration is not ready to apply to Griot.

Read-only checks from a terminal:

```bash
cd /home/artkins/sababisha/projects/gtp/griot
git status --short --branch
git config --get core.hooksPath
python3 scripts/check-contract-sync.py
docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
```

In DBeaver Community: open the existing SQL Server connection, use host `localhost`,
port `14333`, database `Griot`, your configured credentials and Trust server certificate.
Open an SQL editor and run only this read query to establish migration history:

```sql
SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;
```

In Postman: import `backend/Postman/Griot.postman_collection.json` and
`backend/Postman/gtp-2026.postman_environment.json` if not already imported. Once a
verified API process is running, set `baseUrl` to that process's actual URL and send
`GET {{baseUrl}}/health`. HTTP 200 is only a health smoke check, not tenancy acceptance.
Do not run the full mutation collection against valuable data to validate this WIP.

Approval needed now: the spec-29 completion plan and the reconciled Figma Make
amendment. Subsequent migration/operator commands belong in the verified completion
report, after the migration has passed preservation tests.
