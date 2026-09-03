# UI Tokens

## Contracts Synchronization Gate

Any implementation change that corrects or changes a contract must be reflected in the same branch across the affected feature spec, dependent future specs, relevant context files, root `AGENTS.md`, and `progress-tracker.md`. Contracts include design tokens, the MUI/Flutter theme files, and the enum values annotated in Figma (`TaskStatus`, `Priority`, `Role`, `NotificationType`).

## Origin

Tokens are extracted from the **formalized Week-1 Figma design system** (`research/week-01-fundamentals-and-system-design.md` §4.3): the Figma Make output is refined 2–3 rounds, turned into components, then tokenized in Figma proper. These token names are the single source for:
- `web/src/theme.ts` — `createTheme({ palette, typography, shape })`
- `mobile/lib/core/theme.dart` — `ThemeData`
- Figma annotation of status colors (so EF Core enums match the visual model)

## The token table is a *placeholder contract* until the Week-1 Figma file is finalized.

The names below are canonical; the concrete hex/px values get locked in the first project-kit sync session *after* the Week-1 design file is finished. Until then, reference the Figma file directly.

## Color Palette

| Token | Figma role | MUI mapping | Notes |
|---|---|---|---|
| `color.surface.base` | App background | `palette.background.default` | Dark, high-contrast workspace |
| `color.surface.raised` | Cards / columns | `palette.background.paper` | Slightly lighter than base |
| `color.ink.primary` | Primary text | `palette.text.primary` | Near-white on dark |
| `color.ink.secondary` | Secondary text | `palette.text.secondary` | Muted |
| `color.accent.primary` | Brand / primary actions | `palette.primary` | Saturated; the only "brand" accent |
| `color.accent.secondary` | Secondary actions | `palette.secondary` | Rarely used; mostly text links |
| `color.state.success` | Done / healthy | `palette.success` | Also final `TaskStatus` color |
| `color.state.warning` | In review / warnings | `palette.warning` | Also `TaskStatus.InReview` |
| `color.state.danger` | Urgent / overdue | `palette.error` | Also `Priority.Urgent` + overdue due dates |
| `color.state.info` | Info / in-progress | `palette.info` | Also `TaskStatus.InProgress` |

### Status & priority severity map (visual = enum)

| Enum | Value | Color family |
|---|---|---|
| TaskStatus | Backlog | neutral |
| TaskStatus | Todo | neutral/blue |
| TaskStatus | InProgress | info (blue) |
| TaskStatus | InReview | warning (amber) |
| TaskStatus | Done | success (green) |
| Priority | Low | info |
| Priority | Medium | warning |
| Priority | High | danger-orange |
| Priority | Urgent | danger (red) |

## Typography

| Token | Figma role | MUI mapping | Notes |
|---|---|---|---|
| `type.display` | Landing hero / display | `typography.h1` | Web font, 700, tight leading; Public shell only |
| `type.headline` | Page titles | `typography.h4` | 600 weight |
| `type.subhead` | Section titles | `typography.h6` | 600 weight |
| `type.body` | Default text | `typography.body1` | 15–16 px |
| `type.caption` | Meta / chips | `typography.caption` | 12 px, uppercase for status chips |
| `type.mono` | Keys / IDs / timestamps | `typography.fontFamily` mono sparingly | Numbers align better |

- Base font: Inter (web) / default Flutter Roboto → theme `fontFamily`.
- Sizing is 8-pt rhythm-friendly (16 base, 20 title, 28–36 display).

## Spacing & Layout

| Token | Value | Rules |
|---|---|---|
| `space.xs` | 4 px | tight icon gaps |
| `space.sm` | 8 px | inner padding, chip gaps |
| `space.md` | 16 px | default card padding, gap between stacked content |
| `space.lg` | 24 px | section gaps, modal padding |
| `space.xl` | 32–40 px | page gutters, shell gaps |
| Column min/max width | 272 / 320 px | Kanban columns; horizontal scroll within the board |

## Radius

| Token | Value | Use |
|---|---|---|
| `radius.sm` | 4–6 px | Chips, avatars (full for circles), buttons |
| `radius.md` | 8–10 px | Cards, inputs, modals |
| `radius.lg` | 16 px | Empty/loading state illustrations, hero cards |

Denser app surfaces use the smaller end; the Public shell may use `radius.lg` freely.

## Motion

| Token | Value | Use |
|---|---|---|
| `motion.fast` | 120–200 ms | App shell: column reorder, modal, chip state |
| `motion.slow` | 300–500 ms | Public shell GSAP reveals |
| `motion.ease` | cubic-bezier(0.2, 0, 0, 1) | Consistent MUI easing |

App shell never uses springy/elastic easings; MUI theme `transitions` comes from these two tokens.

## Shadows / Elevation

- App shell: 1–2 levels only (`shadow.1` card, `shadow.2` modal/overlay), subtle.
- Public shell: 3 levels, softer ambient glow; never outline-based.

## Rules of Application

1. No component references a raw hex/rgb value. All colors flow through tokens → MUI palette / Flutter `ThemeData`.
2. Status chip colors must match the EF Core enum annotation exactly (contract for `Griot.Domain.TaskStatus`).
3. When the Week-1 Figma token file updates, update this file, `theme.ts`, `theme.dart`, and any Figma annotations in the same change.
4. Before a UI feature ships, grep for hardcoded colors — zero tolerated outside `ui-tokens.md`.