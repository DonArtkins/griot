# Backend Feature 28 — Project Lifecycle Evidence for Reports [own-stack]

**Status:** PLANNED; no schema approval or implementation claimed. One feature branch: `feature/backend/28-project-lifecycle-report-evidence`.

## Type

New feature; the smallest evidence store needed by the supplied CAB, post-deployment and regression templates. It does not replace Jira or add a deployment platform.

## What This Delivers

A project can record deployment readiness, an actual deployment, and test-run evidence. Closing an ordinary task is never treated as proof of deployment or successful testing. Backend 24 uses these records to decide which reports are available; web 11 collects missing inputs.

## Dependencies

Backend 13–17 (workspace/project/task membership), 18 (pagination), 20 (durable audit), 23 (human-only action boundary). Build after backend 11 and before backend 24. Web 11 and ai 07 are consumers, not prerequisites.

## Context To Read First

`research/reports/README.md`, `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`, backend data-layer/API context, approved ERD. Use contract-sync, dotnet-ef-core and documentation-standards skills.

## Files Owned

`backend/src/Griot.Domain/Entities/ProjectEvidence.cs` (proposed name), Application evidence service/DTOs, Infrastructure mapping/repository/migration, Api evidence controller, corresponding xUnit/Postman tests.

## Setup / Initialization

Incorporate the proposal in `docs/planning/AI-SCHEMA-PROPOSALS-2026-09-11.md` into Figma Make and approve/register the ERD export before adding schema. Proposed single append-only table: `ProjectEvidence(Id, WorkspaceId, ProjectId, Kind, RecordedByUserId, RecordedAt, SupersedesId?, RunId?, Page?, TotalPages?, TotalResults?, ContentJson)`. Kind is a validated string registry, not a silent change to `TaskStatus` or `ProjectStatus`. `ContentJson` has `schemaVersion: 1` and the per-kind typed schema below. Index `(WorkspaceId, ProjectId, Kind, RecordedAt, Id)`. Existing projects require no backfill. Run/page metadata is stored in typed columns, not ContentJson: test_run requires a UUID RunId, 1-based Page, positive TotalPages and nonnegative TotalResults (declared result count for the entire run); other kinds require these columns to be null. Enforce Page <= TotalPages and a filtered unique index `(WorkspaceId, ProjectId, RunId, Page)` for test_run rows. Under serialized per-run insertion, reject inconsistent totals, tester, environment, execution timestamp, deployment or baseline references. Duplicate pages return 409; they never overwrite evidence.

## Evidence contracts

| Kind | Required input and validation |
|---|---|
| `deployment_ready` | linked task ID in the same project, requester identity from auth, change title/description, affected modules, PR/build references if available, test evidence references, risk assessment, rollback plan, target environment and requested deployment window with timezone; readiness is explicitly recorded by an authorized human |
| `deployment` | same-project deployment task ID, actual UTC deployment timestamp, environment, build/reference, linked readiness record if available; linked task must be Done; timestamp cannot be inferred from generic task completion |
| `test_run` | environment, executedAt UTC, tester from auth, scope/limitations, optional deployment ID, optional baseline test-run ID, **explicit run identity**: `RunId` (UUID assigned at run start), `Page` (1-based ordinal), `TotalPages` and `TotalResults` (declared at start, immutable across pages). Rows `{caseId, module, scenario, result, severity?, defectTaskId?, evidenceRef?}`; result `pass/fail/blocked/not_run`; severity `critical/high/medium/low` applies only to findings; stable unique case IDs; no guessed outcomes. **Idempotency/uniqueness:** append-only; each `(WorkspaceId, ProjectId, RunId, Page)` is unique — a duplicate page for an existing `RunId` is rejected (409). **Incomplete-run detection before report consumption:** a run is not complete until every declared page (1..TotalPages) is present, the result count equals TotalResults and case IDs are unique across all pages in one consistent read snapshot; a partial or stale run is reported as incomplete/invalidated and is never consumed as a completed test run, without relying on `ContentJson` grouping. |

Validate every linked project/task/evidence reference against membership and the selected project. A failed result without severity remains unclassified until a human supplies it. Corrections append a replacement referencing `SupersedesId`; original evidence remains auditable. Baseline and replacement must have matching project/kind. Correcting test_run evidence appends a complete replacement under a new RunId with page-level SupersedesId provenance; never reuse the original run/page key. An incomplete replacement invalidates report eligibility until its declared pages/results validate. Maximum 1 MB per input record, 1,000 results per page; a run can span pages under one run identity and is only complete after the declared row count is validated. Read pagination is stable `(RecordedAt, Id)`.

## Routes (PLANNED)

- `GET /api/workspaces/{id}/projects/{projectId}/evidence?kind=&cursor=&pageSize=`: scoped member; OBO additionally needs ReadWorkspace and a matching delegation.
- `POST /api/workspaces/{id}/projects/{projectId}/evidence`: workspace Owner/Admin human records evidence; AI OBO 403 before lookup. No AI tool can certify a deployment, test result or sign-off.
- No delete route. Evidence expiry/correction follows the parent project's retention/access policy; revoked membership immediately blocks reads.

## Separation of Concerns

Backend validates and stores facts. QA owns the execution/evidence source; humans enter or import results. AI only summarizes authorized evidence. Web owns the evidence-entry form. No GitHub/Jira connector, code scanner or automatic deployment integration in v1.

## Docker & Deploy

One reviewed SQL Server migration through the existing release procedure; no new database, worker service, container or credential. Test data is local fixtures only; the sample employee/product details are not production seeds.

## Acceptance Criteria

- [ ] An ordinary Done task cannot enable post-deployment reporting; valid deployment evidence can.
- [ ] Missing/foreign task, baseline or evidence references fail without cross-workspace disclosure.
- [ ] A 1,205-case run is assembled across all pages with stable case IDs and exact totals; incomplete runs stay ineligible.
- [ ] Duplicate pages return 409; conflicting totals/metadata, missing pages, count mismatches and repeated case IDs cannot become report-ready, including concurrent uploads.
- [ ] Corrections preserve the original record and record the human actor in AuditLogs/ActivityLogs.
- [ ] AI OBO writes fail 403 even when acting for an Owner; authorized reads retain workspace boundaries.
- [ ] Missing evidence stays unknown; zero tests does not become a passing test run.

## Verification

`dotnet build`, SQL-enabled `dotnet test`, Postman evidence folder and contract-sync. No production data migration until the ERD gate passes.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
