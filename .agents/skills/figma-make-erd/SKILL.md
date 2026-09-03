---
name: figma-make-erd
description: "Generate and formalize the Griot database ERD in Figma Make (the AI prototyping surface, project URL https://www.figma.com/make/bTB7eE6s39O6yvtYfq0DeR/Griot). The approved ERD is the single source of truth for every entity/enum name used by backend, web, mobile, and AI systems."
metadata:
  version: "0.1.0"
---

# Figma Make ERD Skill

## Purpose

The bootcamp's Week-2 deliverable is "Database schema design in Figma". Griot builds the ERD in **Figma Make** (the AI prototyping surface — NOT a plain FigJam board). The live project is:

**https://www.figma.com/make/bTB7eE6s39O6yvtYfq0DeR/Griot?p=f&t=CrPqLvvqtlfVbwSx-0**

The approved ERD in that project is the implementation contract for the backend's EF Core data layer and every client's types.

## When to Use

- Week 2 (create the ERD), or any schema change (ERD-first, approve, then code).

## Process

1. Read `PROMPTS/week-02/01-database-schema-erd-figma-make.md` — the entity roster (13 tables), 4 enums, 15 relationships, and 7 index annotations.
2. Open the Figma Make project URL above. Use the **prompt from the PROMPTS file §5** in Make's AI Describe step (Plan mode for precise steering).
3. Figma Make generates a schema canvas. Refine ≤2 rounds; add shapes/connectors manually where the AI's draft is imprecise so the diagram is an exact contract.
4. Guard: every entity traces to a Week-1 screen — no screen, no entity.
5. Human approves. Export the ERD frame as PNG to `project-kit/diagrams/erd/griot-erd-v1.0.0.png`.
6. Register in `project-kit/diagrams/README.md` ledger (name, version, date, status=approved).
7. Only now may `backend/project-kit/feature-specs/*` transcribe entities. Entity/enum names must match the diagram EXACTLY.

## Rules

- Enums: `TaskStatus` (Backlog/Todo/InProgress/InReview/Done), `Priority` (Low/Medium/High/Urgent), `WorkspaceRole` (Owner/Admin/Member), `NotificationType` (Mention/Assignment/DueDate/System).
- Entity names: `Users`, `Workspaces`, `WorkspaceMembers`, `Invites`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `ActivityLogs`, `Notifications`, `RefreshTokens`.
- A schema change means: update the Make ERD → re-approve → update backend spec 02 + dependent specs + context + progress-trackers together (contract sync gate).
