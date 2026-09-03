---
name: mcp-contract-testing
description: "Contract tests for the Griot MCP server: assert each tool's JSON schema and behavior with a mocked GraphQL client; MCP Inspector smoke run."
metadata:
  version: "0.1.0"
---

# MCP Contract Testing Skill

## Approach

- Each tool = pure function `(graphqlClient, input) -> output`; unit-test the JSON contract with a mocked client.
- Assert input/output schemas (zod) match the root `integration-contracts.md` tool table.

## Runs

```bash
cd mcp && npm test
npx @modelcontextprotocol/inspector node src/server.ts   # manual smoke
```

## Rules

- No network LLM in tests; service token mocked.
- Keep contract tests green in CI (`test-mcp` job).
