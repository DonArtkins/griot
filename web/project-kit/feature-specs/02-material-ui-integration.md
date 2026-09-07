# Feature 02 — Material UI v6 Integration

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"Material UI v6 integration"**: the MUI v6 theme derived from the inspo-synthesized master tokens (`docs/design/MASTER-DESIGN-SYSTEM.md` — light, flat, card-based workspace), base primitives (Button, TextField, Chip, Dialog, Skeleton, Snackbar) with the status/priority severity maps.

## Dependencies

- Web feature 01 (scaffold).
- Week-1 Figma screens finalized; master design tokens locked (`project-kit/context/ui-tokens.md` ← `docs/design/MASTER-DESIGN-SYSTEM.md`).

## Context To Read First

- `web/project-kit/context/design-system.md`
- `ui-registry.md` (basis for the component list)

## Agent Skills To Use

- `web/.agents/skills/material-ui-theme/SKILL.md`

## Files Owned

- `web/src/theme.ts`
- `web/src/components/**` (registry primitives)

## Setup / Initialization

Deps already in feature 01 (`@mui/material @emotion/* @mui/icons-material`).

## Implementation Notes

- `web/src/theme.ts` implements the full inspo-derived token contract (`docs/design/MASTER-DESIGN-SYSTEM.md` → `project-kit/context/ui-tokens.md`): light flat-first palette, Inter + Space Grotesk + IBM Plex Mono (tabular numerals), radius 10/pill/16/20, E0–E3 elevation, `motion.fast` 150ms.
- Primary CTA = `containedPrimary` chrome-ink variant (`#1E2022`); brand blue `#3572F6` is for links/selection/focus only.
- Severity map: `TaskStatus` Backlog=neutral, Todo=primary, InProgress=info, InReview=warning, Done=success; `Priority` Low=info, Medium=warning, High=chart-orange, Urgent=error (exported as `taskStatusColor` / `priorityColor`).
- Build `StatusChip`, `PriorityChip`, `EmptyState`, `LoadingSkeleton`, `ErrorState`, `Toast` as registry components on top of the themed primitives.

## Separation of Concerns

- Theme/registry components are shared; features consume registry, never raw token hex.

## Docker & Deploy

- No change (bundled into the Vercel build).

## Out of Scope

Feature pages (features 06–09).

## Acceptance Criteria

- [ ] `theme.ts` from tokens; zero hardcoded colors in components
- [ ] Chips render per the severity map; empty/loading/error primitives exist
