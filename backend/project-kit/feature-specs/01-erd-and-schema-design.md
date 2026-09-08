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

- `diagrams/erd/griot-erd-v1.0.0.png` (exported, approved)
- `diagrams/README.md` (ledger row)

## Setup / Initialization

No code. Steps: open the Figma Make project → paste PROMPTS §5 into Make's AI (Plan mode) → refine 16 tables + 5 enums + 19 relationships + 13 index notes → approve → export PNG → register in the ledger.

## Implementation Notes

- Entity roster: `Users`, `Workspaces`, `WorkspaceMembers`, `Invites`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `ActivityLogs`, `Notifications`, `RefreshTokens`, `ApiLogs`, `ErrorLogs`, `AuditLogs`.
- Enums: `TaskStatus`, `Priority`, `WorkspaceRole`, `NotificationType`, `ErrorFixStatus` (values per `backend/project-kit/context/data-layer.md`).
- Every entity traces to a Week-1 screen OR an observability requirement (ApiLogs/ErrorLogs/AuditLogs). No screen + no observability requirement, no entity.
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

- [ ] Figma Make project contains all 16 tables (13 core + ApiLogs/ErrorLogs/AuditLogs), 5 enums, 19 relationships, legend, and 13 index notes
- [ ] Names/values match `data-layer.md` exactly
- [ ] Human-approved; PNG exported to `diagrams/erd/griot-erd-v1.0.0.png`
- [ ] Ledger updated


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
