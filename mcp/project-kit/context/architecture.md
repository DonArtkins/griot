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

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
