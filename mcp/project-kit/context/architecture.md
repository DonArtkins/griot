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

## Transports

- stdio: local Claude/Cursor/Cline (`claude mcp add`).
- Streamable HTTP: containerized on Railway (infra feature 06), port 3001; also used by Griot's own agents.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
