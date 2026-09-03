---
name: material-ui-theme
description: "Build the MUI v6 theme from the Figma tokens: palette, typography, shape, transitions. The theme file is the single source of visual style."
metadata:
  version: "0.1.0"
---

# MUI Theme Skill

## Source of truth

`web/project-kit/context/design-system.md` + `project-kit/context/ui-tokens.md` (Week-1 Figma tokens).

## Implementation

```ts
// src/theme.ts
export const theme = createTheme({
  palette: { primary: tokens.accent.primary, background: { default: tokens.surface.base }, … },
  typography: { … },
  shape: { borderRadius: tokens.radius.md },
  transitions: { duration: { standard: tokens.motion.fast } },
});
```

## Rules

- Zero hardcoded colors outside the token module.
- Status colors map exactly to the EF Core enums (TaskStatus/Priority).
- App shell uses `motion.fast`; GSAP is only in the Public shell.
- Register any new visual component in `project-kit/context/ui-registry.md` before use.
