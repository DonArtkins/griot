---
name: docx-pdf-processing
description: "DOCX + PDF processing for Griot AI agents via MCP servers (Model Context Protocol). Parse, read, and extract text from DOCX and PDF documents for research intake and reporting. Uses the community MCP document servers."
metadata:
  version: "0.1.0"
---

# DOCX / PDF Processing Skill

Goal: give Griot's agents the ability to read/extract content from **DOCX** and **PDF** files (bootcamp briefs, research PDFs, generated docs, user uploads) the same way any desktop AI client would.

## The MCP servers (from the internet / Context7 ecosystem)

> **Pin every third-party executable to a reviewed version.** Never run `npx -y <pkg>` / `uvx <pkg>` unpinned — `latest` resolves differently over time and is a supply-chain risk (CWE-829). Resolve the current reviewed version with the Context7 skill, then pin it in the MCP client config AND in a repo manifest (e.g. `.nvmrc`-adjacent `mcp-version.json` or the system's `package.json` devDependencies) so the lockfile records it.

### DOCX — `office-word-mcp-server` (Microsoft Word MCP)

> **Package clarification:** `@microsoft/office-word-mcp-server` is **not a published npm package** — it does not exist on the npm registry. The community reference implementation is `GongRzhe/Office-Word-MCP-Server` (GitHub), which ships as a Python `uvx` tool. An official MCP server for Word documents is `@modelcontextprotocol/server-pdf` (maintained by the MCP core team, `1.7.5`), but that targets PDF. For Word (`.docx`), use the `markitdown-mcp` route (PDF/DOCX → Markdown) or the `OkamiFeng/docx-mcp-server` Python package. **Reviewed version — resolve before use:**

Reviewed versions (re-resolve before each project; record in `mcp-versions.json`):
- `markitdown-mcp` (PyPI/uvx, Microsoft): **`0.0.1a4`** (pre-release; latest as of 2026-09-04 — re-check on each use)
- `@modelcontextprotocol/server-pdf` (npm, MCP core): **`1.7.5`**

Install (MCP client config, e.g. Claude Desktop / Cline) — **pin the version**:

```json
{ "mcpServers": { "word": { "command": "uvx", "args": ["markitdown-mcp@0.0.1a4"] } } }
```

Typical tools exposed: `convert_to_markdown(uri)` — converts DOCX, PDF, XLSX, PPTX, HTML to Markdown over MCP.

### PDF — community PDF MCP servers (several options)

1. **`markitdown`** (Microsoft) — converts PDF/DOCX/XLSX/PPTX to Markdown. Great for turning research PDFs into readable markdown. **Pin the version** (reviewed: `0.0.1a4`):
   ```json
   { "mcpServers": { "markitdown": { "command": "uvx", "args": ["markitdown-mcp@0.0.1a4"] } } }
   ```
2. **`@modelcontextprotocol/server-pdf`** (MCP core, npm) — official MCP PDF server (extract text/pages). **Pin the version** (reviewed: `1.7.5`):
   ```json
   { "mcpServers": { "pdf": { "command": "npx", "args": ["-y", "@modelcontextprotocol/server-pdf@1.7.5"] } } }
   ```
3. **`capacity/mcp-server-pdf`** (Docker) — containerized PDF tools (extract text/pages, OCR); pin the image tag `capacity/mcp-server-pdf:<reviewed-tag>`.
4. **`mcp-server-browser`/general** — for scraping manuscripts (not needed here).

> **Note on `pdf-parse-mcp`:** The package `pdf-parse-mcp` (previously referenced here) does **not exist** as a published npm package. Use `@modelcontextprotocol/server-pdf` instead, or `markitdown-mcp` for combined PDF+DOCX support.

### How to re-resolve versions (with Context7)
- Use the Context7 skill to verify the CURRENT package/tool names + the **reviewed pinned version** before wiring (these packages move); e.g.
  ```bash
  npx ctx7@0.5.9 library "markitdown" "mcp server install version"
  npx ctx7@0.5.9 library "modelcontextprotocol server pdf" "install version"
  ```
- The pattern is the same for all: pick the server that fits the file type, add its `command`/`args` (with the exact pinned version) to the MCP client config, restart the client, and the tools appear.
- Record the resolved version in the feature spec (`Log the tool version used` rule below) **and** in `mcp-versions.json` at repo root.

## When Griot's agents use this

- Reading `research/GTP 2026 BOOTCAMP EDITION.pdf` programmatically (extract → text).
- Generating `docs/` deliverables (Word exports if requested).
- The `qa/` and `docs/` agents turning reports into DOCX/PDF.
- Any `.docx`/`.pdf` upload intake (if the product grows attachments processing).

## Rules

- Prefer **Markdown round-trips**: PDF/DOCX → Markdown → LLM → Markdown → DOCX/PDF (lossless for text; tables/headers preserved via markitdown).
- Never paste private document contents into external tools beyond the MCP server.
- Verify with the Context7 skill before relying on a specific tool name (versions move).
- Log the tool version used in the feature spec (change-management).

## Verify

- A DOCX and a PDF both extract to Markdown without losing headings/tables.
- The MCP client shows the doc tools connected.