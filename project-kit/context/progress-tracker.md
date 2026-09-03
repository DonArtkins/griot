# Progress Tracker

## Contracts Synchronization Gate

Any implementation change that corrects or changes a contract must be reflected in the same branch across the affected feature spec, dependent future specs, relevant context files, root `AGENTS.md`, and this file. A feature is not ready for review while later specs or context still describe stale contracts.

## Current State (Week 2 — Database Schema Design)

**Week 1** has been front-loaded per `research/week-01`: design before code. The Figma Make session outputs are being formalized into a Figma design system, and the entity/screen mapping table is the input to this week's ERD.

- [x] Bootcamp research corpus committed (`research/`, bootcamp-exact stack decisions)
- [x] `PROMPTS/` created and grouped by week; Week-2 ERD prompt written
- [x] `project-kit/` created — context files, feature specs, examples, diagrams folders
- [ ] FigJam ERD generated from `PROMPTS/week-02/01-database-schema-erd-figma.md` and approved
- [ ] ERD PNG exported to `project-kit/diagrams/erd/`
- [ ] Week-2 Definition of Done (see `research/week-02-backend-api-development.md` §8)

## Weekly Status

| Week | Focus | Status | Notes |
|---|---|---|---|
| 1 | Fundamentals & system design | In progress → design formalization | HackerRank SQL/C#; Figma design system; env setup per `gtp-2026-prep.md` §6 |
| 2 | Backend & API | **Current** | DB schema design in Figma → EF Core → REST + GraphQL → Postman |
| 3 | Frontend | Not started | React/Vite/MUI after API is live |
| 4 | Mobile | Not started | Flutter companion surface |
| 5 | Deployment & DevOps | Not started | Docker/Compose, Vercel, Railway, GitHub Actions |
| 6 | Quality engineering | Not started | Test suites, OWASP, k6, 80% gate |
| 7 | Real-world QE | Not started | Manual, UAT, exec summary, portfolio |

## Next Steps

1. **Today:** Generate the ERD in FigJam using the Week-2 prompt in `PROMPTS/`; refine ≤2 rounds; export to `project-kit/diagrams/erd/`.
2. Approve the ERD → it becomes the contract for Feature 03 (`03-database-schema-ef-core-sql-server.md`).
3. Continue Week-2: `.NET` solution scaffold (`Feature 01/02` prerequisites), EF Core transcription, stored procs.
4. Keep `Dockerfile`/Compose parity as the backend takes shape (Feature 02/04).

## Open Questions

- FigJam AI ERD fidelity vs. hand-drawn tables — decide after the first generation pass (prefer hand-drawn table shapes with connectors for precision).
- MUI vs. custom drag-drop library for board reorder in Week 3 (research default: no library, HTML5 drag + optimistic cache; revisit if polish suffers).
- Postgres secondary use cases (cohort exercises only vs. also PR test target) — confirm with cohort lead.

## Session Notes

- **2026-09-02** — Research compacted to bootcamp-exact (`gtp-2026-prep.md` rewrite). Week-1 front-loaded design. Env plan: real Docker Engine, SQL Server 2022 container (14333), Postgres 16 (5433), Redis (6380); .NET 8 per-repo via `global.json`; Node 20 per-repo `.nvmrc`.
- **2026-09-03** — `PROMPTS/` + `project-kit/` scaffolded for Griot modeled on the Foundrie AI repo (AGENTS.md, context files, feature specs, diagrams). Week-2 ERD prompt and FigJam walkthrough written. ERD generation is the immediate deliverable.

## Repository Map

- `research/` — bootcamp research (stack contract, weekly plans, AI integration).
- `PROMPTS/` — week-grouped prompts (Figma/FigJam/agents).
- `project-kit/context/` — this kit's context files.
- `project-kit/feature-specs/` — ordered implementation specs.
- `project-kit/diagrams/` — Figma/FigJam exports that govern implementation (ERD, architecture, wireframes).
- `project-kit/examples/` — reference examples/inspiration assets.