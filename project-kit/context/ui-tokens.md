# UI Tokens (shared design language)

Origin: the Week-1 Figma Make design system, formalized in the **Griot** Figma project. These token names feed the web MUI theme (`web/src/theme.ts`) and the mobile Flutter `ThemeData` (`mobile/lib/core/theme/theme.dart`), and are annotated in Figma so the EF Core enums match the visual model.

The table below is a **placeholder contract** until the Week-1 design file is finalized - the names are canonical, concrete hex/px values get locked at the first post-design sync. Until then, reference the Figma project directly.

| Token | Figma role | MUI / Flutter mapping | Notes |
|---|---|---|---|
| `color.surface.base` | App background | background.default / scaffoldBackgroundColor | dark, high-contrast |
| `color.surface.raised` | Cards / columns | background.paper / Card | slightly lighter |
| `color.ink.primary` | Primary text | text.primary | near-white on dark |
| `color.ink.secondary` | Secondary text | text.secondary | muted |
| `color.accent.primary` | Brand / primary actions | primary | the only brand accent |
| `color.accent.secondary` | Secondary actions | secondary | text links |
| `color.state.success` | Done / healthy | success | == TaskStatus.Done |
| `color.state.warning` | In review / warnings | warning | == TaskStatus.InReview |
| `color.state.danger` | Urgent / overdue | error | == Priority.Urgent + overdue dates |
| `color.state.info` | Info / in-progress | info | == TaskStatus.InProgress |

## Status & priority severity (visual == enum)

TaskStatus: Backlog=neutral, Todo=neutral/blue, InProgress=info, InReview=warning, Done=success. Priority: Low=info, Medium=warning, High=danger-orange, Urgent=danger.

## Typography / spacing / radius / motion

- Type roles: display / headline / subhead / body / caption / mono. Base font Inter (web) / Roboto (Flutter default).
- 8-pt spacing rhythm: xs=4, sm=8, md=16, lg=24, xl=32-40. Kanban column min/max 272/320px.
- Radius: sm=4-6 (chips/buttons), md=8-10 (cards/modals), lg=16 (hero/empty states).
- Motion: fast=120-200ms (app shell), slow=300-500ms + GSAP (public shell); ease cubic-bezier(0.2, 0, 0, 1).
- Elevation: app shell 2 levels; public shell 3 levels, softer.

Rules: no raw colors in components; status chips == enum map; token changes propagate Figma -> theme files -> this doc in one change.
