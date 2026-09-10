# MCP Feature Spec 06 — Report & Audit Tools (v2 roster evolution) [own-stack]

**Status:** PLANNED — lets external AI clients (Claude Desktop, Cursor, VS Code Copilot, Cline) use the same report/audit superpowers as the in-app Copilot (ai 06/07). The v1 9-tool roster (mcp 02) is a **fixed contract** and is NOT changed by this spec; v2 tools are new ids appended after mcp 02 ships and after backend 20/24 exist.

## The v2 tool roster (ids are contracts)

| Tool id | Input (zod) → Output | Backend route |
|---|---|---|
| `list_reports` | `workspaceId, type?, format?, page?` → `{items: ReportMeta[]}` | `GET /api/workspaces/{id}/reports` |
| `get_report` | `workspaceId, reportId` → `ReportDetail` | `GET /api/workspaces/{id}/reports/{reportId}` |
| `generate_report` | `workspaceId, type, window?, promptContext?` → job id (async) | enqueue via `POST /api/webhooks/trigger`; produced by ai 07 |
| `download_report` | `workspaceId, reportId, format` → short-lived download URL (never the blob itself) | `GET .../reports/{id}/download?format=` (spec 24) |
| `system_audit` | `workspaceId, checks?[]` → `AuditSummary` (severity chips) | `GET /api/workspaces/{id}/audit-summary` (spec 24) |
| `get_audit_log` | `workspaceId, entityType?, actorId?, page?` → `AuditLogRow[]` | `GET /api/logs/audit` (spec 20, Owner) |
| `get_metrics` | `workspaceId, window?` → `{velocity, workload, cycleTime, ...}` | dashboard + task reads (spec 16) |

## Guardrails

- Identical to ai 06/07: read-only except report-row creation (`CreateReport`, spec 24); never OTP/auth/delete/invite/member tools; OBO identity from trusted caller context only; RBAC-scoped results (Owner-only data → "insufficient permissions").
- `generate_report` is asynchronous: the tool returns a job id immediately; completion surfaces through `get_report` / the notifications feed (no long HTTP holds).
- Contract-tested as `(graphqlClient, input) → output` with mocked GraphQL (mcp 05 pattern).

## Dependencies

- mcp 02 (v1 roster, unchanged) · backend 20 (audit reads) · backend 24 (reports/download/audit-summary + `CreateReport` scope) · ai 06 (auditor engine) · ai 07 (report pipeline) · infra 04 (deployed transport).

## Implementation notes (PLANNED)

- `reports` + `audit` tool modules wrap the SAME `ai/src/reports/pipeline.ts` (ai 07) and audit engine (ai 06) — one implementation, two surfaces.
- Contract tests extended per v2 tool; a roster allow-list test asserts NO auth/otp/delete/invite/member tool ids exist.
- Streamable HTTP + stdio both expose v2 (no new transport).

## Acceptance Criteria (all PENDING)

- [ ] v1 9-tool roster unchanged until mcp 02 ships; v2 ids are contract-locked by this spec
- [ ] Each v2 tool returns the schema above with RBAC scoping; `generate_report` returns a jobId and never blocks
- [ ] Roster allow-list test: no auth/OTP/delete/invite/member tools
- [ ] MCP Inspector smoke passes for `list_reports`/`get_metrics` against a seeded workspace

## Verification

`npm run lint && npm run typecheck && npm test` (mocked GraphQL); MCP Inspector smoke; Newman + xUnit on the backend routes (post-20/24).

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.