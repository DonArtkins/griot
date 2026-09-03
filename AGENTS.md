# AGENTS.md — Griot Monorepo Orchestrator (GTP 2026 Bootcamp)

## Read This First

You are an AI agent working on **Griot** (*GREE-oh*), a project-management web app built for the **Sababisha Solutions GTP 2026 Bootcamp**. Griot is the "accurate, shared record of what happened and what's next" — workspaces, projects, boards, tasks, comments, notifications, and an AI copilot.

The bootcamp defines the systems; Griot runs exactly on them. **`research/GTP 2026 BOOTCAMP EDITION.pdf` (extracted to `research/_bootcamp_2026.txt`) is the contract.** It defines **five official systems** — Backend/API (Week 2), Web (Week 3), Mobile (Week 4), DevOps (Week 5), Quality Engineering (Weeks 6–7) — and `research/ai-integration.md` adds the **[own-stack] AI system** (Trigger.dev agents + MCP server + Copilot).

**Every system is a self-contained worktree with its own kit.** Do not treat this repo as one app.

## The Seven Systems

| # | System | Folder | Weekly source | Entry point |
|---|---|---|---|---|
| 1 | Backend / API (.NET 8 + SQL Server) | `backend/` | Week 2 | `backend/AGENTS.md` |
| 2 | Web (React 18 + Vite + MUI) | `web/` | Week 3 | `web/AGENTS.md` |
| 3 | Mobile (Flutter 3.19+) | `mobile/` | Week 4 | `mobile/AGENTS.md` |
| 4 | DevOps / Infra (Docker, Vercel, Railway, CI/CD) | `infra/` | Week 5 | `infra/AGENTS.md` |
| 5 | Quality Engineering (tests, OWASP, k6) | `qa/` | Weeks 6–7 | `qa/AGENTS.md` |
| 6 | AI agents (Trigger.dev v3) [own-stack] | `ai/` | `ai-integration.md` | `ai/AGENTS.md` |
| 7 | MCP server [own-stack] | `mcp/` | `ai-integration.md` | `mcp/AGENTS.md` |

## Mandatory Reading Order

1. `research/_bootcamp_2026.txt` + `research/gtp-2026-prep.md` — the contract and environment rules.
2. `research/week-0X-*.md` for the week whose system you are working in.
3. `research/ai-integration.md` for the AI/MCP systems.
4. `project-kit/context/system-map.md` — how the systems communicate.
5. `project-kit/context/stack-contract.md` — the exact stack + [own-stack] markers.
6. `project-kit/context/integration-contracts.md` — cross-system contracts (ports, env, API, GraphQL).
7. The **system's own** `AGENTS.md`, then its `project-kit/context/*`, then its `project-kit/feature-specs/*`.
8. `PROMPTS/` for the Figma Make / FigJam / agent prompts that produce each system's designs (ERD before schema, wireframes before UI).
9. `project-kit/diagrams/**` for approved Figma Make/FigJam artifacts that govern implementation.

## Required Skills

**CRITICAL DIRECTIVE: check `.agents/skills/` and the current system's `.agents/skills/` before any implementation.**

- Root shared skills live in `/.agents/skills/` (contract sync, Figma Make ERD, git branch flow, throttling prevention).
- System skills live in `<system>/.agents/skills/` (dotnet-ef-core, hotchocolate, mui, riverpod, trigger-dev, newman, k6…).
- Read the relevant `SKILL.md` and follow it exactly. Do not rely on memory for current APIs.

## Cross-System Rules (Hard Rules)

0. **Contract synchronization is a hard gate.** A change to any cross-system contract (API route, GraphQL type, env var, port, entity/enum, auth token shape, `GRIOT_SERVICE_TOKEN`, Docker service name) must be reflected in the owning system's feature spec, all dependent systems' specs, the relevant context files, and the root `AGENTS.md` at once. Never leave a system describing a stale contract.
1. **Separation of concerns is physical.** `backend/` owns data + API; `web/` + `mobile/` own presentation; `ai/` + `mcp/` own intelligence; `infra/` owns containers + deployment; `qa/` owns test lifecycles. No system writes code into another system's folder.
2. **AI never writes to SQL Server directly.** Every AI read/write goes through the .NET API via `GRIOT_SERVICE_TOKEN` (resolved to a restricted `ai-agent` principal).
3. **The PDF stack is never substituted silently.** Every deviation must carry `[own-stack]` and a written rationale in `project-kit/context/stack-contract.md`.
4. **ERD before schema, wireframes before UI.** The approved Figma Make ERD (`project-kit/diagrams/erd/`) is the only source for entity/enum names; no schema code may exist before it is approved.
5. **Plan before implementation.** Present a concrete plan and wait for explicit approval before schema migrations, API surface changes, Docker/Compose changes, or deployment changes.
6. **One feature spec at a time, in numeric order.** Never batch specs. A spec is done only when its tests/build/CI gates pass and contracts are synchronized.
7. **Throttling prevention.** See `.agents/skills/throttling-prevention/SKILL.md`. Batch reads/writes, prefer shell for bulk ops, pause on throttling.

## Verification Gates (per system)

- **backend**: `dotnet build` + `dotnet test` green; compose local stack healthy.
- **web**: `npm run lint && npm run typecheck && npm test && npm run build` green.
- **mobile**: `flutter analyze && flutter test` green.
- **ai/mcp**: `npm run lint && npm run typecheck && npm test` green (golden transcripts, MCP contract tests — no LLM in CI).
- **infra**: `docker compose up` reproduces the full local topology; CI jobs green.
- **qa**: xUnit, Jest+RTL, Flutter, Cypress, Newman green; coverage ≥80% (service layer + auth); k6 baseline recorded; OWASP logged.

## Stack at a Glance

Backend: .NET 8, ASP.NET Core Web API, EF Core 8, Dapper 2.x, HotChocolate 14+, SQL Server 2022, PostgreSQL 16. Web: React 18.3, Vite 5, MUI v6, Apollo, Axios, TanStack Query 5. Mobile: Flutter 3.19+, Dart 3, GraphQL Flutter, Riverpod. DevOps: Docker 26+, Compose v2, Vercel, GitHub Actions, Railway/Render/Azure. Auth [own-stack]: JWT + Argon2 + Redis. AI [own-stack]: Trigger.dev v3, MCP.

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
