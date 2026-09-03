# UI Context

## Contracts Synchronization Gate

Any implementation change that corrects or changes a contract must be reflected in the same branch across the affected feature spec, dependent future specs, relevant context files, root `AGENTS.md`, and `progress-tracker.md`. Contracts include design tokens, component names, route names, and the entity/enum values that the UI annotates (TaskStatus, Priority, Role, NotificationType).

## Design System Foundation

The design system originates in the **Week-1 Figma Make sessions** (see `research/week-01-fundamentals-and-system-design.md`) and is formalized in Figma into components + tokens before any code. The tokens are transcribed into:
- the **MUI theme** (`web/src/theme.ts` → `createTheme`), *not* a separate Tailwind config;
- the **Flutter theme** (`mobile/lib/core/theme.dart` → `ThemeData`);
- Figma annotation of status colors so the EF Core `TaskStatus`/`Priority` enums match the visual model.

`ui-tokens.md` holds the actual token values; `ui-rules.md` holds behavior; `ui-registry.md` holds the component list. This file describes the two shells they all hang off.

## The Two Shells

Griot ships two deliberately different user experiences:

### 1. Public Shell (the Awwwards face)
- Surfaces: Landing (`/`), Pricing (`/pricing`), Login (`/login`), Signup (`/signup`).
- Goal: spectacle + conversion — best-converting SaaS sites show real product UI in the hero, place trust signals at decision points, treat sub-2-second load as a conversion lever.
- Motion: GSAP + Lenis via dynamic import; this is the *only* surface with heavy animation.
- Measured in Week 6/7 with Lighthouse; accessibility (axe) is a hard gate.

### 2. App Shell (the product)
- Surfaces: Dashboard, Kanban Board, Task Detail, Team Settings, Notifications.
- Goal: speed + clarity of team coordination. No marketing polish beyond status clarity and calm motion.
- Motion: MUI transitions only (fast, <200 ms); no GSAP.
- Both shells share the same MUI token theme; the App shell reuses the Public shell's landing-imported fonts/colors but never its animation budget.

## Screen → Entity Mapping (the schema guard)

| Screen | Entities implied | Derived from |
|---|---|---|
| Dashboard | `Workspace`, `Project`, `ActivityLog` (feed) | Week-1 §4.4 |
| Kanban board | `Board`, `Column`, `TaskItem` (status, priority, dueDate, assigneeId) | Week-1 §4.4 |
| Task detail | `TaskItem` + `Comment`, `Attachment` | Week-1 §4.4 |
| Team settings | `User`, `WorkspaceMember` (role enum), `Invite` | Week-1 §4.4 |
| Notifications | `Notification` (type, read state, target ref) | Week-1 §4.4 |
| (AI surface) | `ActivityLog` reuse as audit + `summarize_project` source | `ai-integration.md` |

If an entity is not in this table, it is not in the v1 design — and therefore not in the v1 schema.

## Status & Priority Semantics (visual = enum)

The Figma annotation fixes the mapping before code:

- **TaskStatus**: Backlog · Todo · In Progress · In Review · Done. Each has a column color and a "current task" chip color.
- **Priority**: Low · Medium · High · Urgent. Rendered as severity-tinted chips/avatars (Urgent = same red family as overdue dates).
- **WorkspaceRole**: Owner · Admin · Member.
- **NotificationType**: Mention · Assignment · DueDate · System.

The enum names above are the exact C#/Dart/TS identifiers. Renaming a status in Figma after implementation triggers the contracts sync gate.

## Visual Anchors

- Dark, high-contrast workspace (calm chrome, saturated data accents) so high-priority and overdue states pop without noise.
- Consistent 8-pt spacing rhythm; radius scale small-er on dense app surfaces than the marketing shell.
- Empty/loading/error states are designed assets, never grey rectangles: the Week-1 screens define them and the Week-3 build ships them.

## Design Governance

1. Every token change goes through `ui-tokens.md` first, then `theme.ts` / `theme.dart` — never ad-hoc colors in components.
2. Every new component is registered in `ui-registry.md` before it is used in pages.
3. Board interactions (drag-drop) exist only on web; mobile uses a status picker — the mobile-idiomatic equivalent.
4. The Copilot panel (AI surface) is a collapsible right-rail thread that renders MUI chat bubbles and mutation-approval cards; it must never block board interactivity while streaming.