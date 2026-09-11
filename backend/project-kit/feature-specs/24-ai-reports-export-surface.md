# Backend Feature 24 — Governed Reports, Generation Jobs and Exports [own-stack]

**Status:** PLANNED. Existing `Report` rows do not imply that generation, download or approval workflows exist. One feature branch: `feature/backend/24-ai-reports-export-surface`.

## Type

New backend report surface using existing Reports storage, lifecycle evidence and blob storage.

## What This Delivers

Workspace-scoped, lifecycle-valid report templates rendered as PDF/CSV. Human members create/list/get/download reports within their data tier; only Owner/Admin humans delete. AI OBO may request generation/upload for an authorized job only after the separately audited fifth scope CreateReport ships. AI never deletes or signs off a report.

## Dependencies

Backend 07/09, 11 (private blob artifacts), 18 (pagination), 20 (durable jobs/audit/idempotency), 22 (completion notice), 23 (human-only actions), 28 (lifecycle evidence). Backend 25 later applies the platform tier matrix before P0 closes. Ai 07, web 11 and mcp 06 are consumers, not backend prerequisites.

## Context To Read First

`research/reports/README.md`, backend 28, `docs/planning/AI-SYSTEM-AUDIT-2026-09-11.md`, approved ERD and artifact contract. Skills: dotnet-ef-core, Context7, contract-sync, documentation-standards.

## Files Owned

Backend report/job DTOs/services/controllers, Report mapping/migration and Postman/xUnit contracts. AI owns deterministic aggregation and rendering; backend validates and persists the structured result.

## Setup / Initialization

Existing entity: `Report(Id, WorkspaceId, Type, GeneratedBy, ContentJson, GeneratedAt, PromptContext?)`. Proposed additions after ERD approval: `Title`, `CreatorUserId`, `Status` (`queued/running/completed/failed`), `DeletedAt`, plus an **immutable `CreatedAt` (UTC, set once on row insert)** that anchors stable `(CreatedAt, Id)` cursors for list/aggregation pagination (GeneratedAt is the generation timestamp and is not the row-creation anchor). `ContentJson.schemaVersion = 1` holds project/evidence/source IDs and revisions, template version, UTC window, consistency/coverage metadata, typed aggregates, narrative and separate private blob references for PDF/CSV. GeneratedBy is display provenance; authorization uses backend-resolved CreatorUserId and workspace, never a client-supplied string. No arbitrary artifact URL is accepted or fetched. Migration `AddReportArtifacts` adds CreatedAt, backfills existing rows from GeneratedAt once, then requires it on every insert; application updates and job completion cannot change it. Migration runs only after the proposal in `docs/planning/AI-SCHEMA-PROPOSALS-2026-09-11.md` is incorporated into an approved Figma Make ERD and its exported diagram is registered.

## Template registry and eligibility

| Type key | Evidence required before generation |
|---|---|
| `cab_deployment_request` | backend 28 readiness record, same-project task, affected modules, test summary, risk, rollback and requested window |
| `qa_test` | complete test run with stable case IDs/results and scope; QA board completion alone is insufficient |
| `post_deployment` | actual deployment evidence + Done deployment task + defined observation window after deployment; checks/log coverage explicitly identified |
| `regression` | baseline run/report plus retest run mapping the same case IDs; added/omitted/not-run cases shown, not silently treated as passes |
| `sprint_digest`, `task_summary`, `velocity`, `workload` | permitted task/board history for the requested interval; trend/velocity stays unavailable when historical evidence is missing |
| `ai_action_summary` | recorded executed plan steps and outcomes, not an invented success narrative |

No unbounded `custom` freeform report in v1. Role/access reminders are notices under backend 27, not report types. Eligibility is checked server-side on options, generation and completion; clients cannot force a phase-ineligible report by calling the API directly. Missing inputs produce structured reasons. A report can contain explicit unknown sections; missing deployment/test prerequisites cannot yield a completed formal report.

## Routes (PLANNED)

| Method | Route | Gate / behavior |
|---|---|---|
| GET | `/api/workspaces/{id}/projects/{projectId}/report-options` | member + data tier; valid types and missing prerequisites, no inaccessible-row counts |
| GET | `/api/workspaces/{id}/reports?type=&page=&pageSize=` | member + artifact/source data tier, capped stable pagination |
| POST | `/api/workspaces/{id}/reports` | member or OBO CreateReport; `{projectId,type,title,window,evidenceIds,formats}` creates queued row + durable job, returns 202 with reportId/status URL |
| GET | `/api/workspaces/{id}/reports/{reportId}` | authorized metadata, structured result or job status |
| POST | `/api/workspaces/{id}/reports/{reportId}/artifacts?format=pdf\|csv` | bound generation job + CreateReport, allowed format/size only; upload through backend 11, no direct AI blob credentials |
| GET | `/api/workspaces/{id}/reports/{reportId}/download?format=pdf\|csv` | reauthorize every request; stream correct private artifact; no permanent public links |
| DELETE | `/api/workspaces/{id}/reports/{reportId}` | Owner/Admin human only; soft-delete + queued blob cleanup |
| GET | `/api/workspaces/{id}/audit-summary?window=24h&severity=` | Owner initially; Owner/Admin after backend 25; sanitized workspace aggregate, not raw AuditLogs |

MCP generate_report uses the POST reports route. The backend stores request identity and enqueues with its own TRIGGER_SECRET_KEY; MCP never receives webhook or Trigger credentials. A generation result is a spec-20 HMAC callback bound to the persisted report/job/workspace and source snapshot. Reject mismatched, duplicated or expired callbacks. Completion is recorded only after both requested artifacts and structured validation succeed.

## Data correctness and lifecycle

Fetch every page beyond the 1,000-row cap with each source's stable ordering/cursors: reports use `(CreatedAt, Id)` and backend 28 evidence uses `(RecordedAt, Id)`; aggregate only authorized rows. At job creation freeze a source manifest/revision and cutoff. Test-run inputs must pass backend 28 completeness checks over typed RunId/Page/TotalPages/TotalResults columns; a partial, conflicting or superseded run cannot enable a completed formal report. Immutable event/evidence rows use that cutoff. Mutable task state is version-checked; a changed source invalidates/restarts the snapshot or produces an explicit incomplete result, never an unnoticed mixture of versions. Keep counts in typed aggregation code; model prose cannot alter them. Telemetry loss, retention limits and unclassified severities appear in coverage metadata.

CAB approval and QE sign-off are human assertions backed by recorded evidence. An AI-generated document cannot mark itself CAB-approved, invent signatures or change project/task status. Regression compares the referenced baseline case IDs rather than whichever tasks happen to be visible today. Private raw-log-derived content retains its higher data tier for reads/downloads; membership alone cannot expose it. Saved charts and links have the same checks as their parent report. No public embedding/sharing in v1.

## Separation of Concerns

Backend owns permission/eligibility checks, snapshot manifest, durable job state and private artifact delivery. AI renders from typed inputs. Web records human evidence and previews results. Report creation is an explicit request or a previously authorized schedule; report generation does not approve deployment or send a bulk notice.

## Docker & Deploy

Reuse SQL Server, backend 11 blob service and Trigger cloud. Durable outbox retries failed enqueue; no 202 without a persisted queued job. No new service. Artifact cap 20 MB per file. Add CreateReport to the backend grant validator, endpoint allowlist and capability resolver together; do not grant it to every executor by default.

## Acceptance Criteria

- [ ] Direct generation of post-deployment/regression without required evidence is rejected even if the UI is bypassed.
- [ ] Human member can create/list/get/download permitted reports; Owner/Admin can delete; OBO delete is 403 before lookup.
- [ ] OBO without CreateReport cannot create/upload; valid request returns 202 only after durable job persistence.
- [ ] 1,205-row fixtures include every row, exact totals and stable source provenance; concurrent change is detected.
- [ ] Every report action is audited; only completed generation sends a requester completion notification, once. Reads/downloads/deletes send none.
- [ ] Both formats belong to one Report; downloads recheck access, private tier and deletion state.
- [ ] CSV text neutralizes leading =, +, -, @ after whitespace/control-character normalization; typed numeric fields remain numbers; RFC 4180 UTF-8 fixtures pass.
- [ ] No AI-generated approval/signature or invented test result appears; known coverage gaps are visible.
- [ ] Callback replay, enqueue failure and partial artifact upload recover without duplicate reports/notifications.

## Verification

`dotnet build`, SQL-enabled `dotnet test`, Postman eligibility/download/replay/tier tests and contract-sync. Ai 07 golden render fixtures are a later integration gate.

## Multi-Tenant Update (2026-09-11 — PLANNED)

- **Reports become per-org scoped:** `Report` rows are stamped with `OrganizationId` (resolved via the workspace's org) and every create/list/get/download/delete runs inside `ITenantContext` — EF global query filters (spec 37) make cross-org reads empty; the workspace-scoped and org-scoped boundaries compose (org → workspace → data tier).
- **Client-facing progress report type (PLANNED, spec 34 view)** joins the template registry — evidence-gated like every other template; client downloads recheck `ProjectClients` attachment and never expose internal board internals (dedicated client-view DTO/fields only).
- **Blob artifacts under `org/{orgId}/reports/…`** (spec 11's Multi-Tenant Update) — private download links recheck org + tier + deletion state on every fetch.
- **CreateReport (fifth OBO scope, spec 09/44 revisions)** is capability-gated within the OBO user's org: delegation `OrganizationId` must match the active tenant and the target workspace's org — a mismatch is rejected before job creation; AI still never deletes or signs off a report.
- Aggregation/pagination queries gain `@OrganizationId` (spec 38 revision); `(CreatedAt, Id)` cursors unchanged; suspended/offboarding org **freezes generation** (org lifecycle interplay — writes 403 `org_suspended`).
- `audit-summary` reports read the org-stamped `AuditLogs`/`ActivityLogs` (spec 51 revision) and feed spec 25's per-tenant tiers; platform (NULL-org) rows are never aggregated into org reports.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
