# Project Griot — Project Overview

## Contracts Synchronization Gate

Any implementation change that corrects or changes a contract must be reflected in the same branch across the affected feature spec, dependent future specs, relevant context files, root `AGENTS.md`, and `progress-tracker.md`. Contracts include EF Core entities/relations and enum values, REST route signatures, GraphQL type/query/mutation names, `GRIOT_SERVICE_TOKEN` behavior, env variables, Docker/Compose service names and ports, storage paths, and file ownership. A feature is not ready for review while later specs or context still describe stale fields or old API shapes.

## Overview

Griot is a project-management web app (a "PM tool") built for the GTP 2026 Bootcamp on the bootcamp-mandated stack. The product metaphor is the **griot** — a West African oral historian / record-keeper: "the accurate, shared record of what happened and what's next" is exactly a PM tool's value proposition. In-universe, Griot is also Wakanda's AI system (Shuri's lab assistant that tracks every invention and holds accumulated knowledge).

Griot delivers: one workspace → projects → boards → columns → tasks, with comments, attachments, roles/invites, an activity feed, notifications, and an AI copilot (Trigger.dev + MCP) that summarizes and proposes writes against the same API.

The division of responsibility: **the bootcamp PDF owns what and why (the stack contract); the research corpus owns how; the agents own implementation; the human owns approval.** Nothing substitutes the bootcamp stack unless marked `[own-stack]`.

## Product Positioning

Griot is the capstone product for the GTP 2026 programme. It demonstrates the full bootcamp stack working end-to-end (SQL Server → .NET/GraphQL → React/Flutter → Vercel/Railway → GitHub Actions), and adds a portfolio-grade differentiator: an AI layer (in-app Copilot, proactive agents, and an MCP server) that *wraps* the mandated stack without replacing it.

## Mental Model

Screens in Figma (Week 1) → ERD in FigJam (Week 2) → EF Core schema transcription → REST + GraphQL API → React web + Flutter mobile → deployed with Docker/Vercel/Railway → gated by a full QE chain (Week 6–7).

**Design governs data:** the entity model is derived from the five core screens, never invented separately. If an entity has no corresponding screen, it is not in the v1 schema.

## Primary Users

- **Team members** — assign and track tasks on boards, comment, attach files, receive notifications.
- **Workspace owners/admins** — create projects, manage team membership and invites with roles.
- **Themselves as AI users** — ask the Copilot "what's blocked this week?", draft tasks, get digests.

## Core User Flow

1. User signs up / in (custom JWT, Argon2, Redis refresh rotation).
2. User creates a workspace, invites teammates by email (roles: Owner/Admin/Member).
3. User creates a project; the project spawns a board with columns (Backlog / In Progress / In Review / Done).
4. User creates tasks (title, description, assignee, priority, due date, attachments), moves them across columns.
5. Comments and changes fan out to the Activity Feed and Notifications (mention, assignment, due-date reminder).
6. The Copilot panel answers questions about the board and proposes mutations the user must approve.

## Core Features (v1)

- **Workspace management** — one workspace per user in v1, team membership with roles, email invites.
- **Projects → Boards → Columns → Tasks** — the core nesting; board drag-drop on web, status picker on mobile.
- **Task detail** — description, assignee, priority, due date, comment thread, attachments.
- **Activity feed** — who did what, when (also the AI layer's audit + `summarize_project` source).
- **Notifications** — mention, assignment, due-date reminder; read/unread state.
- **Web (React/Vite/MUI) + Mobile (Flutter) surfaces** — same backend, two clients.
- **AI layer** — Copilot chat, scheduled digests/reminders/stale-board detection, MCP tools (`get_board`, `create_task`, …).

## Out of Scope (v1)

Timelines/Gantt, custom fields, guest access, automations, multi-board reports, multi-workspace-per-user, payments/billing, iOS builds (no Xcode on Parrot), real-time multiplayer presence, public API for third parties.

## Architecture at a Glance

- **Client layer**: `web/` (Vite + React 18 + MUI; Public shell + App shell) and `mobile/` (Flutter + Riverpod).
- **API layer**: `backend/` ASP.NET Core 8 — REST controllers + HotChocolate GraphQL sharing one service layer (`Griot.Application`).
- **Data layer**: SQL Server 2022 (primary, EF Core 8 + Dapper), PostgreSQL 16 (secondary), Redis (rate limit + refresh tokens). Containers named `gtp-*`, non-default host ports.
- **AI layer**: `ai/` (Trigger.dev v3) + `mcp/` (Griot MCP server) — both talk *only* through the API with a scoped service token.
- **Deployment**: Docker/Compose locally; Vercel (web), Railway (backend + mcp), Trigger cloud (ai); GitHub Actions gates every PR.

## Success Criteria

1. The Week-1 Figma screens are transcribed into an ERD (FigJam) and then into EF Core without inventing entities.
2. SQL Server 2022 container accepts migrations; `dotnet build` clean; REST + GraphQL both live in one process.
3. A signed-in user can complete the core loop on web (create project → add tasks → drag to Done) and on mobile.
4. Auth is owned code: Argon2 hashing, rotated refresh tokens, Redis rate limiting — auditable in Week 6.
5. The AI layer reads and proposes writes only through the API; no AI path touches SQL Server directly.
6. Full deployment: web on Vercel, backend + MCP on Railway, AI on Trigger — all from GitHub Actions.
7. QE gate: xUnit/Jest/Flutter/Cypress/Newman green, k6 baseline recorded, OWASP review logged, ≥80% coverage.
8. Project documentation (this kit + diagrams) is complete enough that any agent can build the next feature with zero ambiguity.

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**