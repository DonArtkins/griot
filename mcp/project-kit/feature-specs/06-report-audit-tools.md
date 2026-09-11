# MCP Feature Spec 06 — Report & Audit Tools (v2 roster evolution) [own-stack]

**Status:** PLANNED — lets external AI clients (Claude Desktop, Cursor, VS Code Copilot, Cline) use the same report/audit superpowers as the in-app Copilot (ai 06/07). The v1 8-tool roster (mcp 02) is a **fixed contract** and is NOT changed by this spec; v2 tools are new ids appended after mcp 02 ships and after backend 20/24/25 exist (get_audit_log reads the backend-25 sanitized workspace audit surface).

## The v2 tool roster (ids are contracts)

| Tool id | Input (zod) → Output | Backend route |
|---|---|---|
| `list_reports` | `workspaceId, type?, format?, page?` → `{items: ReportMeta[]}` | `GET /api/workspaces/{id}/reports` |
| `get_report` | `workspaceId, reportId` → `ReportDetail` | `GET /api/workspaces/{id}/reports/{reportId}` |
| `generate_report` | `workspaceId, type, window?, promptContext?` → job id (async) | `POST /api/workspaces/{id}/reports` (backend 24): 202 only after durable job persistence; OBO CreateReport + matching workspace grant; server-side enqueue, never the callback webhook |
| `download_report` | `workspaceId, reportId, format` → authenticated backend download reference (no public blob URL) | `GET .../reports/{id}/download?format=` (spec 24) |
| `system_audit` | `workspaceId, checks?[]` → `AuditSummary` (severity chips) | `GET /api/workspaces/{id}/audit-summary` (spec 24) |
| `get_audit_log` | `workspaceId, entityType?, actorId?, page?` → `AuditLogRow[]` | backend 25 sanitized workspace audit-summary for Owner/Admin; raw `/api/logs/audit` only with SuperAdmin/Dev capability |
| `get_metrics` | `workspaceId, window?` → `{velocity, workload, cycleTime, ...}` | dashboard + task reads (spec 16) |

## Guardrails

- Identical to ai 06/07: read-only except report-row creation (`CreateReport`, spec 24); never OTP/auth/delete/invite/member tools; **per-transport OBO identity source** (stdio = spawning process's bound user; Streamable HTTP = bearer-bound session) — the server rejects client- or model-supplied identity overrides; RBAC-scoped results (data tier + workspace grant enforced at source).
- **Cross-user authorization test:** a session bound to user A cannot read user B's threads/reports or resolve user B's audiences (403).
- `generate_report` is asynchronous: the tool returns a job id immediately; completion surfaces through `get_report` / the notifications feed (no long HTTP holds).
- Contract-tested as `(graphqlClient, input) → output` with mocked GraphQL (mcp 05 pattern).

## Dependencies

- mcp 02 (v1 roster, unchanged) · backend 20 (audit reads) · backend 25 (manifest/data tiers) · backend 28 (evidence) · backend 24 (reports/download/audit-summary + `CreateReport` scope) · ai 06 (auditor engine) · ai 07 (report pipeline) · infra 04 (deployed transport).

## Implementation notes (PLANNED)

- `reports` + `audit` tool modules call backend REST/GraphQL only. MCP never imports AI pipeline code, holds provider secrets or triggers Trigger.dev directly.
- Contract tests extended per v2 tool; a roster allow-list test asserts NO auth/otp/delete/invite/member tool ids exist.
- Streamable HTTP + stdio both expose v2 (no new transport).

## Acceptance Criteria (all PENDING)

- [ ] v1 8-tool roster unchanged until mcp 02 ships; v2 ids are contract-locked by this spec
- [ ] Each v2 tool returns the schema above with RBAC scoping; `generate_report` returns a jobId and never blocks
- [ ] Roster allow-list test: no auth/OTP/delete/invite/member tools
- [ ] MCP Inspector smoke passes for `list_reports`/`get_metrics` against a seeded workspace

## Verification

`npm run lint && npm run typecheck && npm test` (mocked GraphQL); MCP Inspector smoke; Newman + xUnit on the backend routes (post-20/24).

## Setup / Initialization

Use mcp 01’s exact dependency pins and mcp 03’s transport-bound identity. Fetch the backend capability manifest before registering tools; generate_report requires explicit confirmation/provenance and the CreateReport grant. Use contract-sync, mcp-contract-testing and Context7 skills.

## Separation of Concerns

MCP validates input and delegates to backend endpoints. Backend owns eligibility/jobs/artifacts; AI renders in its own project. Missing evidence and unknown metrics remain explicit.

## Docker & Deploy

Existing stdio and HTTPS Streamable HTTP transports; no new container, provider or secret. Poll report status through .NET only.

## Multi-Tenant Update (2026-09-11 — PLANNED)

- Per-org audit-summary: `system_audit`/`get_audit_log` aggregate within the active org only (observability tables gain nullable `OrganizationId` — backend 51/29); no cross-org rollups.
- Client progress report variant: a new report type on the backend 24/34 surface whose audience is the Client tier — client-scoped data only (progress view, milestones, no internal board internals), same async `generate_report` job path.
- `list_reports`/`get_report`/`download_report` resolve org from the delegated principal; org B report ids from an org A session are denied before dispatch.
- Client-tier session manifests expose client-portal read tools only (mcp 07 v3 roster per backend 25).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.