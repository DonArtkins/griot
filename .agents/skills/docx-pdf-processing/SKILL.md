---
name: docx-pdf-processing
description: "DOCX + PDF processing for Griot AI agents via MCP servers (Model Context Protocol). Parse, read, and extract text from DOCX and PDF documents for research intake and reporting. Uses the community MCP document servers."
metadata:
  version: "0.1.0"
---

# DOCX / PDF Processing Skill

Goal: give Griot's agents the ability to read/extract content from **DOCX** and **PDF** files (bootcamp briefs, research PDFs, generated docs, user uploads) the same way any desktop AI client would.

## The MCP servers (from the internet / Context7 ecosystem)

### DOCX — `office-word-mcp-server` (Microsoft Word MCP)

> Microsoft maintain a reference MCP server for Word: `microsoft/office-word-mcp-server` (npx package `@microsoft/office-word-mcp-server`). It exposes tools to create/read/edit Word documents over MCP.

Install (MCP client config, e.g. Claude Desktop / Cline):

```json
{ "mcpServers": { "word": { "command": "npx", "args": ["-y", "@microsoft/office-word-mcp-server"] } } }
```

Typical tools exposed: create/edit/read a Word document, insert paragraphs, tables, etc.

### PDF — community PDF MCP servers (several options)

1. **`markitdown`** (Microsoft) — converts PDF/DOCX/XLSX/PPTX to Markdown. Great for turning research PDFs into readable markdown.
   ```json
   { "mcpServers": { "markitdown": { "command": "uvx", "args": ["markitdown-mcp"] } } }
   ```
2. **`pdf-parse`** (pure text extraction) — `npx -y pdf-parse-mcp` or `npx @anhthang/pdf-mcp`.
3. **`capacity/mcp-server-pdf`** (Docker) — containerized PDF tools (extract text/pages, OCR).
4. **`mcp-server-browser`/general** — for scraping manuscripts (not needed here).

### How to "install from Context7"
- Use the Context7 skill to verify the CURRENT package/tool names + transport before wiring (these packages move); e.g.
  ```bash
  npx ctx7 library "markitdown" "mcp server install"
  npx ctx7 library "office word mcp" "install"
  ```
- The pattern is the same for all: pick the server that fits the file type, add its `command`/`args` to the MCP client config, restart the client, and the tools appear.

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