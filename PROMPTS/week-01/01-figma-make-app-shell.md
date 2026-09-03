# Week 01 · Prompt 01 — Figma Make: App Shell (5 Core Screens)

**Tool:** Figma Make → **Plan mode**. **When:** Week 1, before any code.
**Research:** `research/week-01-fundamentals-and-system-design.md` §4.2–4.4.

## Context to load

Griot is a project-management web app. Scope for v1 (must be included in the prompt):
- One workspace per user; Workspaces → Projects → Boards → Columns → Tasks
- Task: title, description, assignee, priority, due date, status, comment thread, attachments
- Team membership with roles (owner/admin/member) + email invites
- Activity feed + Notifications (mention, assignment, due-date reminder)

## Prompt (paste into Figma Make — Plan step)

> Design the **App Shell** for **Griot**, a project-management web app for small teams. Dark, high-contrast, calm workspace; saturated accents only for priority and status.
>
> Create exactly these 5 screens, consistent across all of them (same sidebar, same header, same component language):
>
> 1. **Dashboard** — workspace title, project cards (name, progress, last activity), recent activity feed on the right.
> 2. **Kanban Board** — 4 columns (Backlog, In Progress, In Review, Done), task cards showing title, priority chip, due date, assignee avatar, comment count; column headers show a count.
> 3. **Task Detail** (modal over the board) — description, assignee, priority selector, due date, comment thread, attachment list.
> 4. **Team Settings** — member list with role badges (Owner/Admin/Member), invite-by-email field, pending invites.
> 5. **Notifications** — list of items with type icons (mention, assignment, due date), read/unread state, "mark all read".
>
>
> Also design the **empty states** (no projects, empty column) and **loading states** (skeleton) for the dashboard and board.
>
> Do NOT include: timelines/Gantt, custom fields, guest access, automations, multi-board reports, or a mobile layout.

## Refinement (2–3 rounds)

1. First pass is a *rough clay model* — review for scope leaks and clarity, then regenerate/point-correct.
2. Enforce: same left sidebar + top bar across all 5 screens; status colors consistent (In Progress=blue, In Review=amber, Done=green).
3. Final round = consistency pass, then move into Figma proper.

## Done → handoff

- Export screens; in Figma turn TaskCard, BoardColumn, Sidebar, Modal, NotificationItem into **components**.
- Extract tokens per `project-kit/context/ui-tokens.md` (fill the placeholder token table).
- Complete the screen→entity table in `research/week-01` §4.4 — it is the Week-2 ERD input.
- Screenshots archived in `project-kit/diagrams/ui/`.