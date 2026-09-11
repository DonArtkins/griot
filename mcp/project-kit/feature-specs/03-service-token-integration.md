# Feature 03 - Service-Token Integration

## Type

NEW FEATURE ([own-stack])

## What This Delivers

The real GraphQL client wired with `GRIOT_SERVICE_TOKEN` + the `X-On-Behalf-Of` header, so every tool executes against the backend as the restricted **real-user On-Behalf-Of (OBO)** principal (role `ai-on-behalf-of` — spec 09).

## Dependencies

- Feature 02. Backend feature 09 (token handler) + 05 (GraphQL).

## Context To Read First

- `mcp/project-kit/context/security.md`

## Files Owned

- `mcp/src/lib/graphql.ts`

## Files

CREATE: typed GraphQL client with Bearer `GRIOT_SERVICE_TOKEN` plus per-request `X-On-Behalf-Of` from trusted caller context, query/mutation helpers, and error mapping. Reject missing real-user identity before dispatch; never take it from model-generated tool arguments.

## Implementation Notes

- Token from env (`GRIOT_API_URL`, `GRIOT_SERVICE_TOKEN`); never logs it.
- All tool calls now round-trip through the backend - audit rows flow into ActivityLogs.

## Separation of Concerns

- Transport/auth isolated in `lib/graphql.ts`; tools stay pure by receiving the client.

## Docker & Deploy

- Env added to the container runtime (feature 04).

## Acceptance Criteria

- [ ] Tools execute against the backend with the service token + `X-On-Behalf-Of` (real-user OBO principal); write tools respect the four scopes (ReadWorkspace/CreateTask/AddComment/CreateNotification — no deletes/invites)
- [ ] Failed auth surfaces clean MCP errors


## Trusted MCP identity and writes

**PLANNED transport binding:** stdio is bound to one operator-configured real user and workspace in the local MCP profile; an unbound profile fails closed. Streamable HTTP public endpoints require HTTPS/TLS and a per-user authenticated session mapped server-side to that user/workspace; a shared transport bearer alone is not a user identity. No client header/tool argument/model output may replace that identity. The backend independently requires its unexpired `ServiceToken:Delegations:{userId}` grant (workspace IDs + scopes + UTC expiry) and live membership. Internal `http://mcp:3001` is only the private container hop.

Each transport must test session A attempting to supply B's user/workspace identity, including report IDs and tool arguments: reject before dispatch; no cross-user result or count leakage. MCP writes require recorded user confirmation bound to tool, exact arguments/hash and identity; a client claim that an action is confirmed is insufficient. Until verified approval provenance exists, write tools remain unregistered. Never expose auth/OTP/delete/invite/member/status-update tools. `update_task_status` has no issued scope and is removed from the planned roster; do not map it to CreateTask.

## Multi-Tenant Update (2026-09-11 — PLANNED)

- The OBO delegation gains `OrganizationId` (backend 29/30 bump): `ServiceToken:Delegations:{userId}` carries workspace IDs + scopes + the active org; every tool executes scoped to that org.
- Trusted-identity rule extended to tenants: identity AND org come from the server-resolved delegation — never from model-generated tool arguments; an org A principal asking for org B data is rejected at the backend before dispatch (no client-side filtering).
- Raw-log tools stay SuperAdmin/Dev tier only per backend 25: Admin/PM/Member/Client principals get the sanitized audit surface or a denial, never raw `/api/logs/audit`.
- Contract mirrored in `mcp/project-kit/context/security.md` + integration-contracts when backend 29 ships; PLANNED — no code change yet.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
