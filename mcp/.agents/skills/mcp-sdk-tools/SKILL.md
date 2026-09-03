---
name: mcp-sdk-tools
description: "Build the Griot MCP server with @modelcontextprotocol/sdk: zod-validated tools, stdio + Streamable HTTP transports, and GraphQL-backed execution."
metadata:
  version: "0.1.0"
---

# MCP SDK Tools Skill

## Server

```ts
// mcp/src/server.ts
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
export const server = new McpServer({ name: "griot", version: "0.1.0" });

server.tool("get_board", { boardId: z.string() }, async ({ boardId }) => {
  const data = await graphql.query(getBoardQuery, { boardId }, { token: await serviceToken() });
  return { content: [{ type: "text", text: JSON.stringify(data) }] };
});
```

## Transports

- `stdio` (local Claude/Cursor/Cline), `Streamable HTTP` (Docker/Railway + web).
- Tool I/O validated with Zod; responses are JSON text content.

## Rules

- Tools only through the backend GraphQL with `GRIOT_SERVICE_TOKEN`.
- Keep tools as pure functions `(graphqlClient, input) -> output` for testability.
