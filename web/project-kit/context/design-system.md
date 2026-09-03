# Web Design System

Derived from the Week-1 Figma design system; token values live in `project-kit/context/ui-tokens.md` (root). The MUI theme (`src/theme.ts`) is the single implementation.

- **Palette**: dark high-contrast workspace; saturated accents only for priority/status. Status color map = the ERD enums (InProgress=blue, InReview=amber, Done=green; Urgent=red).
- **Typography**: Inter; `display/headline/body/caption` roles from tokens.
- **Spacing**: 8-pt rhythm; `space.xs–xl`.
- **Radius**: `sm` chips · `md` cards/modals · `lg` hero/empty states.
- **Motion**: `motion.fast` (120–200ms) in the App shell; `motion.slow` + GSAP in Public.
- **Registry**: components in `project-kit/context/ui-registry.md` (root) — new components register before use.

Rules: no raw colors/spacing outside tokens; status chips == enum values; every async surface ships empty/loading/error states.
