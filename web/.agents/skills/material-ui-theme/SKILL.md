---
name: material-ui-theme
description: "Build the MUI v6 theme from the Griot master design tokens (inspo-derived): palette, typography, shape, transitions, component overriders. The theme file is the single source of visual style."
metadata:
  version: "0.2.0"
---

# MUI Theme Skill

## Source of truth

`project-kit/context/ui-tokens.md` + `docs/design/MASTER-DESIGN-SYSTEM.md` (inspo provenance) + `web/project-kit/context/design-system.md`.

`web/src/theme.ts` already implements the full token contract — extend it, never fork it.

## Implementation

```ts
// src/theme.ts (already in place)
import { theme, tokens, taskStatusColor, priorityColor, softFill } from './theme';
```

- Palette, typography, shape, transitions and component overriders are pre-wired from tokens.
- Custom styling reads `theme.griot` (typed onto the MUI `Theme`) — never hardcode hex values.
- `taskStatusColor` / `priorityColor` are the severity maps == the EF Core enums.
- `softFill(hex)` produces the ~14%-alpha chip/badge background (text on chip = base color).

## Rules

- Zero hardcoded colors outside the token module.
- Status colors map exactly to the EF Core enums (TaskStatus/Priority).
- Primary CTA is the `containedPrimary` chrome-ink variant; brand blue is for links/selection/focus.
- App shell uses `motion.fast` (150ms); GSAP is only in the Public shell.
- Register any new visual component in `project-kit/context/ui-registry.md` before use.
