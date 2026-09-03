# Feature 02 — Material UI v6 Integration

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable **"Material UI v6 integration"**: the MUI v6 theme derived from the Week-1 Figma tokens, re-themed dark workspace, base primitives (Button, TextField, Chip, Dialog, Skeleton, Snackbar) with the status/priority severity maps.

## Dependencies

- Web feature 01 (scaffold).
- Week-1 Figma tokens finalized (`project-kit/context/ui-tokens.md`).

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

- `createTheme`: palette from tokens; typography Inter; shape radii; transitions `motion.fast`.
- Severity map: `TaskStatus` InProgress=info, InReview=warning, Done=success; `Priority` Urgent=error.
- Build `StatusChip`, `PriorityChip`, `EmptyState`, `LoadingSkeleton`, `ErrorState`, `Toast` as registry components.

## Separation of Concerns

- Theme/registry components are shared; features consume registry, never raw token hex.

## Docker & Deploy

- No change (bundled into the Vercel build).

## Out of Scope

Feature pages (features 06–09).

## Acceptance Criteria

- [ ] `theme.ts` from tokens; zero hardcoded colors in components
- [ ] Chips render per the severity map; empty/loading/error primitives exist
