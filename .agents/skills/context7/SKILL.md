---
name: context7
description: "Context7 — up-to-date code documentation for LLMs/AI editors. Query current docs for any library/framework/CLI (e.g. /vercel/next.js, /tailwindlabs/tailwindcss.com). Use for EVERY implementation that depends on a library's current API. Installable as CLI (ctx7) or MCP server (@upstash/context7-mcp)."
metadata:
  version: "0.1.0"
---

# Context7 Skill

Pulls up-to-date, version-specific documentation and code examples straight from the source and places them into the prompt — fighting outdated training data and hallucinated APIs.

## Install (CLI) — pin the version

Reviewed versions (re-resolve before each project; record in `mcp-versions.json` at repo root):
- `ctx7` CLI: **`0.5.9`** (npm package `ctx7`, NOT `@upstash/context7-cli` — that scope does not exist)
- `@upstash/context7-mcp` MCP server: **`4.0.4`**

```bash
# npm global install (pinned) — CLI package is `ctx7`, not @upstash/context7-cli:
npm install -g ctx7@0.5.9
# verify:
ctx7 --version
```

```bash
# one-off via npx (pinned):
npx -y ctx7@0.5.9 --version
```

## Install (MCP server — Claude Desktop / Cline / Cursor) — pin the version

Add to the MCP client config (e.g. `~/.config/Claude/claude_desktop_config.json` or Cline MCP settings):

```json
{ "mcpServers": { "context7": { "command": "npx", "args": ["-y", "@upstash/context7-mcp@4.0.4"] } } }
```

> **Security rule (CWE-829):** never run `npx -y @upstash/context7-mcp` unpinned. The reviewed versions above were resolved 2026-09-04 from https://github.com/upstash/context7/releases. Re-resolve before each project and record the result in `mcp-versions.json` (a repo-root manifest tracking all MCP tool versions). Do not rely on `latest`.

MCP exposes two tools:
- `resolve-library-id` — turn a general library name into a Context7-compatible ID
- `query-docs` — fetch docs for a library ID + question

## Usage

```bash
# 1. find the library ID
npx ctx7 library "Entity Framework Core" "<specific question>"

# 2. fetch docs
npx ctx7 docs <libraryId> "<specific implementation question>"
```

Known useful IDs (verify with step 1 — do not rely on memory):
- `/vercel/next.js` · `/tailwindlabs/tailwindcss.com` · `/mui/material-ui`
- `/apollographql/apollo-client` · `/tanstack/query` · `/facebook/react`
- `/prisma/prisma` · `/kodcu/refik` (no — use `ctx7 library` to resolve)
- HotChocolate: `ctx7 library "hotchocolate graphql"`
- Trigger.dev: `ctx7 library "trigger.dev"`
- MCP SDK: `ctx7 library "@modelcontextprotocol/sdk"`

## Rules (from Foundrie + this repo)

- **Always** use Context7 before writing code that depends on a library's current API (EF Core migrations CLI, HotChocolate resolver API, GraphQL Flutter providers, Trigger.dev task syntax, MCP SDK transports).
- Never paste secrets/keys into Context7 queries.
- Before committing package versions, check Context7 + official sources for the current stable + compatibility.
- Record important version-specific findings in the relevant feature spec or context file.
- If Context7 results conflict with a context file, pause → research → update the context file → implement.

## Verify

- `ctx7 library` returns real library IDs; `ctx7 docs` returns current docs with version info.
- The MCP server shows as connected in the client (tools `resolve-library-id`, `query-docs`).