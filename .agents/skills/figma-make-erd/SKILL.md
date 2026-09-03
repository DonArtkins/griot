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

1. Read `PROMPTS/week-02/02-erd-figma-make-2000char-sequence.md` — the six seeded prompts (A→F), each **≤2000 chars** (Figma Make's AI prompt cap). One full prompt can't hold 13 tables + 4 enums + 15 edges + indexes.
2. Open the Figma Make project URL above. Paste **PROMPT A** → generate (Plan mode first) → then paste **PROMPT B** on the SAME canvas → C → D → E → F. Each follow-up adds/updates the live result (Figma Make supports this); do NOT start a fresh canvas per prompt, or the ERD resets.
3. After F, use a **fix snippet** from that file for any drift (rename field, recolor module, redraw edge) — one tiny prompt, not a re-invite of the whole spec.
4. Guard: every entity traces to a Week-1 screen — no screen, no entity. Names/values must match `backend/project-kit/context/data-layer.md` and the prompt set exactly.
5. Human approves. Export the ERD frame as PNG to `project-kit/diagrams/erd/griot-erd-v1.0.0.png`.
6. Register in `project-kit/diagrams/README.md` ledger (name, version, date, status=approved).
7. Only now may `backend/project-kit/feature-specs/*` transcribe entities. Entity/enum names must match the diagram EXACTLY.

## Prompt hygiene (2000-char budget)

- Compact rows: `Field type badge` with `|` separators; no filler sentences.
- Paste the **fence contents only** (the prompt text), never the `###` headers or backticks.
- If a prompt is still over the cap, split it further (e.g. Prompt C into Social and Observability halves) rather than deleting fields.

## Rules

- Enums: `TaskStatus` (Backlog/Todo/InProgress/InReview/Done), `Priority` (Low/Medium/High/Urgent), `WorkspaceRole` (Owner/Admin/Member), `NotificationType` (Mention/Assignment/DueDate/System).
- Entity names: `Users`, `Workspaces`, `WorkspaceMembers`, `Invites`, `Projects`, `Boards`, `Columns`, `TaskItems`, `Comments`, `Attachments`, `ActivityLogs`, `Notifications`, `RefreshTokens`.
- A schema change means: update the Make ERD → re-approve → update backend spec 02 + dependent specs + context + progress-trackers together (contract sync gate).
