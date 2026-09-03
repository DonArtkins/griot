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
