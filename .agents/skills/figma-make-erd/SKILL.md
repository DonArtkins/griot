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

1. Read `PROMPTS/week-02/02-erd-figma-make-master-prompt.md` — the **single master prompt** with all 16 tables, 5 enums, 19 relationships, indexes, and conventions. Paste it as a complete unit (see the bounded-capacity note below).
2. Open the Figma Make project URL above. Paste the master prompt into Make's AI prompt → generate (Plan mode first). One full generation; if the canvas truncates any table, use the short add-on snippet in that file (never re-paste everything).
3. After generation, iterate with the fix snippets in that file (rename field, recolor module, redraw edge) — small targeted prompts, not a re-invite of the whole spec.
4. Guard: every entity traces to a Week-1 screen or an observability requirement. Names/values must match `backend/project-kit/context/data-layer.md` and the master prompt exactly.
5. Human approves. Export the ERD frame as PNG to `project-kit/diagrams/erd/griot-erd-v1.0.0.png`.
6. Register in `project-kit/diagrams/README.md` ledger (name, version, date, status=approved).
7. Only now may `backend/project-kit/feature-specs/*` transcribe entities. Entity/enum names must match the diagram EXACTLY.

## Prompt hygiene (bounded capacity — not infinite)

- **Do NOT claim "no length limit".** Figma Make has bounded prompt capacity (50,000-char limit for URL-prefilled prompts; pasted prompts have dynamic limits). Prompts are written extensive and complete, but capacity is finite — preserve full field lists, relationship labels, and index annotations; trim only filler, never schema.
- **Bounded completeness gate (before approval):** after generation, compare the canvas against a required checklist: **16 tables, 5 enums, 19 labelled relationships, 13 index stickies, legend, conventions note**. ANY omission → add content with a targeted add-on snippet (name the table/enum/edge + "as in my first prompt") or reject the generation. Never approve an incomplete ERD.

## Rules

- Enums (5): `TaskStatus` (Backlog/Todo/InProgress/InReview/Done), `Priority` (Low/Medium/High/Urgent), `WorkspaceRole` (Owner/Admin/Member), `NotificationType` (Mention/Assignment/DueDate/System), `ErrorFixStatus` (Open/Investigating/Fixed/Verified/WonTFix).
- Entity names: `Users`, `Workspaces`, `WorkspaceMembers`, `Invites`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `ActivityLogs`, `Notifications`, `RefreshTokens`.
- A schema change means: update the Make ERD → re-approve → update backend spec 02 + dependent specs + context + progress-trackers together (contract sync gate).
