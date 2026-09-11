# MCP Architecture

```
mcp/
├── package.json          # own lockfile, Node 20
├── src/
│   ├── server.ts         # McpServer (name: griot)
│   ├── transports.ts     # stdio + StreamableHTTP
│   ├── tools/            # one file per tool (pure functions)
│   └── lib/graphql.ts    # GraphQL client with GRIOT_SERVICE_TOKEN
└── tests/                # contract tests (mocked GraphQL)
```

Data flow: external AI client <-> MCP <-> backend GraphQL <-> SQL Server. The MCP server holds no DB credentials and no LLM keys.

The planned GraphQL client sends `X-On-Behalf-Of: {real User.Id}` alongside Bearer
`GRIOT_SERVICE_TOKEN`, using trusted caller context rather than model-generated
arguments. Missing identity fails closed. Backend 09 resolves role `ai-on-behalf-of`
with exactly ReadWorkspace/CreateTask/AddComment/CreateNotification; no synthetic
workspace member or fifth scope is created. MCP implementation remains planned.

## Transports

- stdio: local Claude/Cursor/Cline (`claude mcp add`).
- Streamable HTTP: containerized on Railway (infra feature 06), port 3001; also used by Griot's own agents.

## Implemented authentication contract (Feature 07)

Use the [auth contract](../../../docs/api/auth-contract.md) for current routes, status codes, JWT claims,
configuration, token lifetime and storage. `FamilyId` is preserved on rotation;
replay revokes only the same user/family. Registration returns 201 after SQL
persistence; malformed refresh returns 401 and authenticated logout remains 204.


**v2 roster (spec 06, PLANNED):** `list_reports`, `get_report`, `generate_report`, `download_report`, `system_audit`, `get_audit_log`, `get_metrics` — same implementations as ai 06/07 via backend 20/24; v1 8-tool roster unchanged until mcp 02 ships; never OTP/auth/delete/invite/member tools.

## Trusted MCP identity and writes

**PLANNED transport binding:** stdio is bound to one operator-configured real user and workspace in the local MCP profile; an unbound profile fails closed. Streamable HTTP public endpoints require HTTPS/TLS and a per-user authenticated session mapped server-side to that user/workspace; a shared transport bearer alone is not a user identity. No client header/tool argument/model output may replace that identity. The backend independently requires its unexpired `ServiceToken:Delegations:{userId}` grant (workspace IDs + scopes + UTC expiry) and live membership. Internal `http://mcp:3001` is only the private container hop.

Each transport must test session A attempting to supply B's user/workspace identity, including report IDs and tool arguments: reject before dispatch; no cross-user result or count leakage. MCP writes require recorded user confirmation bound to tool, exact arguments/hash and identity; a client claim that an action is confirmed is insufficient. Until verified approval provenance exists, write tools remain unregistered. Never expose auth/OTP/delete/invite/member/status-update tools. `update_task_status` has no issued scope and is removed from the planned roster; do not map it to CreateTask.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
