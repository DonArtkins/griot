# Backend Feature 25 — Role-Tiered Logs and AI Capability Gateway [own-stack]

**Status:** PLANNED. Backend 09 already rejects raw-log reads for AI; this spec adds explicit platform tiers before any such tool is exposed. One feature branch: `feature/backend/25-role-tiered-log-access-ai-capability-gateway`.

## Type

New authorization/data projection feature.

## What This Delivers

The backend resolves tools from the real user's current authority, requested workspace and delegated scopes. A forbidden tool is absent from the manifest and still rejected if invoked directly. Prompt instructions and UI visibility are never authorization controls.

## Dependencies

Backend 09 (service delegation/allowlist), 16/18 (scoped reads/pagination), 20 (writers), 24 (reports). Ai 06/09, mcp 06 and web 11/12 are consumers, not prerequisites.

## Context To Read First

`docs/api/ai-service-token-contract.md`, root integration contract, approved ERD and `docs/planning/AI-SCHEMA-PROPOSALS-2026-09-11.md`. Skills: contract-sync, dotnet-ef-core, Context7.

## Files Owned

Backend role/capability policy, sanitized log DTOs, manifest controller, User mapping/migration and security tests. Client code belongs to its own future specs.

## Setup / Initialization

Approve proposed `User.SystemRole` (`User`, `SuperAdmin`, `Dev`) before migration. Existing users default to User. Workspace Owner/Admin/Member stays in WorkspaceMembers; do not add a second global Admin role. Platform roles are provisioned by an operator outside public registration/AI; no self-promotion route. Current workspace-Owner raw access in spec 16 is a transitional implemented contract, not the target security model and not launch-ready.

## Access matrix

| Caller | Full raw logs/stack traces | Scoped audit projection | Activity feed | AI summary |
|---|---|---|---|---|
| Normal member | never | never | own workspace | plain-language, permitted facts |
| Workspace Owner/Admin | never | administered workspace, sanitized DTO | own workspace | health/member activity within scope |
| Platform SuperAdmin/Dev | privileged access | authorized scope | authorized scope | technical details through explicit capability |

Raw AuditLogs rows stay in the privileged tier. Workspace audit is a separate projection omitting stack traces, tokens, request bodies, request-level forensics and unrelated PII. Aggregates must not reveal the count or existence of inaccessible records. Raw tools for AI also require ReadWorkspace, an active delegation for the selected workspace, and the OBO user's platform tier; the service token itself never confers platform authority. Platform-wide diagnostics without a workspace-bound delegation remain human-only.

## Routes (PLANNED)

- `GET /api/me/capabilities?workspaceId=`: live manifest `{toolId, allowedOperations, dataTier, workspaceId}` filtered by role, membership, scopes and implemented backend capabilities. Planned/unimplemented tools are never advertised.
- `GET /api/logs/errors` and `/api/logs/audit`: SuperAdmin/Dev only; update the old Owner/Admin contract, Postman and clients atomically when this spec ships.
- `GET /api/logs/summary?workspaceId=&window=`: workspace Owner/Admin or authorized platform user; redacted health only.
- `GET /api/workspaces/{id}/audit-summary`: spec 24 aggregate expanded from Owner to Owner/Admin; safe DTO, not raw rows.
- Workspace audit projection lives under the audit-summary contract; do not create a second raw-logs route for Admins.

## Separation of Concerns

Application policy resolves capabilities; API and GraphQL enforce it at data access; AI/MCP consume the same manifest. Model tool selection and external client arguments cannot override identity or tier. Cross-user transport tests are required in mcp 03/06.

## Docker & Deploy

Existing backend migration/release only; no new container or configurable role allowlist that could accidentally grant raw access to User/Admin. Platform-role bootstrap is an operator runbook step.

## Acceptance Criteria

- [ ] Raw logs are 403 for members and workspace Admin/Owners in direct REST and AI tool calls.
- [ ] Admin audit/health output contains only authorized redacted workspace data; no stack/requestId-level forensic data.
- [ ] SuperAdmin/Dev explicit raw capability works only within delegated AI workspace scope; normal member OBO never inherits service privileges.
- [ ] A forged tool ID bypassing the manifest still fails before data is returned.
- [ ] Membership/role/grant revocation affects the next request; cached artifacts and tool lists cannot retain access.
- [ ] No capability claims auth/OTP/member/delete support or an unimplemented endpoint.

## Verification

`dotnet build`, SQL-enabled `dotnet test`, Postman tier matrix, contract-sync. Include direct endpoint calls as well as manifest tests.

## Multi-Tenant Update (2026-09-11 — PLANNED)

- **Role tiers extended with a Client tier + client capability gateway:** a client OBO principal (`OrganizationRole.Client` via `ProjectClients`, spec 34) resolves only `client.read_progress` / `client.add_feedback` capabilities — progress-only, feedback-only; no internal task/board tool ever enters the client manifest, and ai 13 consumes the same restricted manifest.
- **`log.read_tier` permission keys gate org-tier log reads:** per-tenant reads resolve from the spec-51 revision's org-stamped `ApiLogs`/`ErrorLogs`/`AuditLogs`/`ActivityLogs`; an org Admin gets the sanitized audit projection **within own org only**; platform (NULL-org) rows never leak to org readers — **raw logs stay SuperAdmin/Dev** (unchanged).
- **Capabilities manifest gains org context:** `{toolId, allowedOperations, dataTier, organizationId, workspaceId}` — resolved from the caller's org role + the delegation's `OrganizationId`; a cross-org tool invocation is rejected **before** any data access.
- OBO delegation gains `OrganizationId` (spec 44 revision); the service token itself still never confers platform authority; client OBO reads use only client-scoped data (ai 13's boundary).
- Suspended/offboarding org: capability resolution still runs (reads allowed) but write capabilities are suppressed by the `org_suspended` gate (spec 32/39 revisions).
- Cross-user transport tests (mcp 03/06) gain cross-org cases; aggregates still must not reveal the existence or count of inaccessible records.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
