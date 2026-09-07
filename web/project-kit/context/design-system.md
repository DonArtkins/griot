# Web Design System

Derived from the **Griot master design system** (`docs/design/MASTER-DESIGN-SYSTEM.md`), synthesized from `inspo/` (1–12); token values live in `project-kit/context/ui-tokens.md`. The MUI theme (`src/theme.ts`) is the single implementation.

- **Canvas**: light, flat, card-based — `#F7F8FA` canvas, white cards with 1px `#ECECEC` hairlines, no shadows at rest (inspo 4, 8). Dark is a derived variant, not the default.
- **Palette**: blue `#3572F6` = links/selection/focus; the primary CTA is dark chrome `#1E2022` (inspo 1, 10). Saturated accents (blue/violet/pink/info/orange/success) appear only on state chips, deltas, meters and charts. Status color map = the ERD enums (InProgress=info `#0BC1E6`, InReview=warning `#FF7F1C`, Done=success `#47BA39`; High=orange `#F59E0B`, Urgent=danger `#F14C43`).
- **Typography**: Inter (UI) + Space Grotesk (display/KPI) + IBM Plex Mono (numerals, tabular-nums); `display/headline/body/caption/overline/kpi` roles from tokens; overlines 11px uppercase +0.6em (inspo 4, 8).
- **Spacing**: 8-pt rhythm; `space.xs–3xl`; grid gaps 8 (dense) / 16 (default); 44px list/table rows.
- **Radius**: 10 buttons/inputs · pill chips/badges · 16 cards/popovers · 20 modals.
- **Elevation**: flat-first — E0 none (hairline cards), E1 hover, E2 popover, E3 modal; neutral shadows only.
- **Motion**: `motion.fast` 150ms `cubic-bezier(0.2,0,0,1)` in the App shell; `motion.slow` + GSAP in Public.
- **Registry**: components in `project-kit/context/ui-registry.md` (root) — new components register before use.

Rules: no raw colors/spacing outside tokens; status chips == enum values; every async surface ships empty/loading/error states.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
