# Griot — Project Management with an AI Copilot

**Griot** (*GREE-oh*) is a project-management web app for small teams — the "accurate, shared record of what happened and what's next". Workspaces → projects → boards → tasks, with comments, attachments, roles & invites, an activity feed, notifications — and an AI copilot that summarizes the board, drafts tasks, and acts on approval.

> Built for the **Sababisha Solutions GTP 2026 Bootcamp** on the bootcamp-exact stack, plus an [own-stack] AI layer (Trigger.dev agents + MCP server).

## The seven systems (one monorepo)

| System | Folder | Stack |
|---|---|---|
| Backend / API | `backend/` | .NET 8 · ASP.NET Core Web API · EF Core 8 · Dapper · HotChocolate · SQL Server 2022 · Postgres 16 · Redis |
| Web | `web/` | React 18 · Vite 5 · MUI v6 · Apollo · Axios · TanStack Query 5 · Zustand |
| Mobile | `mobile/` | Flutter 3.19+ · Dart 3 · Riverpod · graphql_flutter · dio |
| DevOps / Infra | `infra/` | Docker 26+ · Compose v2 · Vercel · Railway · GitHub Actions |
| Quality Engineering | `qa/` | xUnit · Jest+RTL · Flutter tests · Cypress · Newman · k6 · OWASP |
| AI agents | `ai/` | Trigger.dev v3 (Copilot + scheduled agents) — [own-stack] |
| MCP server | `mcp/` | @modelcontextprotocol/sdk — [own-stack] |

Each system is self-contained (own `AGENTS.md`, `.agents/skills/`, `project-kit/`). The root `AGENTS.md` + `docs/ARCHITECTURE.md` link them into one system.

## Status

**Planning / system-design phase** — no production code yet. See `docs/planning/`, `PROMPTS/`, and `project-kit/`.

- Docs: `docs/ARCHITECTURE.md`, `docs/database/DATABASE-DESIGN.md`, `docs/planning/` (NFR, capacity, risk, runbook, change-management)
- Diagrams: 12 design diagrams specified in `PROMPTS/week-02/` → to be generated in **Figma Make**
- ERD: 16 tables / 5 enums (decided pre-implementation — no schema rework later)

## Getting Started

### First-time setup (after git clone)

Binary files (PDFs, PowerPoints, screenshots) are stored in Cloudinary to avoid bloating the Git repository. Download them with:

```bash
./scripts/cloudinary-download.sh
```

This restores all research documents and screenshots (~15 MB, 14 files). See `docs/tooling/CLOUDINARY-BINARY-FILES.md` for details.

**What you get:**
- `research/GTP 2026 BOOTCAMP EDITION.pdf` — Official bootcamp spec
- `research/Netdata_RD_Presentation.pptx` — Monitoring strategy
- `research/screenshots/*.png` — Deployment evidence & UI references

## Quick links

- **Docs**: `docs/README.md` · **Architecture**: `docs/ARCHITECTURE.md` · **Database**: `docs/database/DATABASE-DESIGN.md`
- **Diagrams**: `project-kit/diagrams/README.md` · **Prompts**: `PROMPTS/README.md`
- **Contribute**: `CONTRIBUTING.md` · **Security**: `SECURITY.md` · **License**: `LICENSE` (MIT)

## Roadmap (bootcamp)

Week 1 Fundamentals · Week 2 Backend & API · Week 3 Frontend · Week 4 Mobile · Week 5 Deploy & DevOps · Week 6 QE foundations · Week 7 Real-world QE. Details per week in `research/`.

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
_Griot — the record of what the team built, and how well they built it._