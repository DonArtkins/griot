# Feature 01 — Database Schema Design in Figma Make (ERD)

## Type

NEW FEATURE

## What This Delivers

The bootcamp's Week-2 deliverable **"Database schema design in Figma"**: an approved entity-relationship diagram built in **Figma Make** (the AI prototyping surface) that is the single source of truth for every entity/enum name used by backend, web, mobile, and AI. No schema code exists before this is approved.

**Live project:** `https://www.figma.com/make/bTB7eE6s39O6yvtYfq0DeR/Griot?p=f&t=CrPqLvvqtlfVbwSx-0`

## Dependencies

- Week-1 design system + screen→entity table (`research/week-01-fundamentals-and-system-design.md` §4.4).
- Figma account with Figma Make access (same account as Week 1).

## Context To Read First

- Root `AGENTS.md` + `/.agents/skills/figma-make-erd/SKILL.md`
- `research/week-02-backend-api-development.md`
- `PROMPTS/week-02/01-database-schema-erd-figma-make.md` (master spec + walkthrough)
- `PROMPTS/week-02/02-erd-figma-make-master-prompt.md` (single extensive master prompt — no length limit; the working prompt)

## Agent Skills To Use

- `/.agents/skills/figma-make-erd/SKILL.md`

## Files Owned

- `project-kit/diagrams/erd/griot-erd-v1.0.0.png` (exported, approved)
- `project-kit/diagrams/README.md` (ledger row)

## Setup / Initialization

No code. Steps: open the Figma Make project → paste PROMPTS §5 into Make's AI (Plan mode) → refine 13 tables + 4 enums + 15 relationships + 7 index notes → approve → export PNG → register in the ledger.

## Implementation Notes

- Entity roster: `Users`, `Workspaces`, `WorkspaceMembers`, `Invites`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `ActivityLogs`, `Notifications`, `RefreshTokens`.
- Enums: `TaskStatus`, `Priority`, `WorkspaceRole`, `NotificationType` (values per `backend/project-kit/context/data-layer.md`).
- Every entity traces to a Week-1 screen. No screen, no entity.
- Crow's-foot cardinality; module color codes; legend; index sticky notes.

## Separation of Concerns

The ERD is a **design artifact** produced in the design tool, owned by the *database concern*. It is the input to `Griot.Domain` entities (feature 02) — the diagram and the code are two sides of the same contract.

## Docker & Deploy

Not applicable (design deliverable).

## Out of Scope

Any SQL, migrations, or entity code (feature 02).

## Future Modifications

- Feature 02 transcribes this ERD into EF Core.
- Any schema change re-enters through this artifact (ERD-first).

## Acceptance Criteria

- [ ] Figma Make project contains all 13 tables, 4 enums, 15 relationships, legend, and index notes
- [ ] Names/values match `data-layer.md` exactly
- [ ] Human-approved; PNG exported to `project-kit/diagrams/erd/griot-erd-v1.0.0.png`
- [ ] Ledger updated
