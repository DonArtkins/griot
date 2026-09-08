# Mobile Design System

Derived from the **Griot master design system** (`docs/design/MASTER-DESIGN-SYSTEM.md`), synthesized from `inspo/` (1–12); token values live in `project-kit/context/ui-tokens.md`. The Flutter theme (`lib/core/theme/theme.dart`) is the single implementation — `GriotColors`/`GriotRadii` theme extensions carry the full token set.

- **Canvas**: light, flat, card-based — `#F7F8FA` canvas (`surfaceContainer`), white radius-24 cards with 1px `#ECECEC` hairlines (inspo 9, 11; flat-first per 4, 8).
- **Palette**: identical token set to web. Blue `#3572F6` = links/selection/full-width CTA on mobile (9, 11 Submit/Continue); chrome-ink `#1E2022` for the dark action pill (10 Stop Charging) and toasts. Status map == the ERD enums (`taskStatusColor`/`priorityColor` in the theme).
- **Header**: tint gradient `#E4E6F7 → canvas.base` (`griotHeaderTint`), centered title, white 44px circular back/close buttons (9, 11).
- **Typography**: Inter (UI) + Space Grotesk (display/KPI hero numerals); 14/20 body, 13/18 meta, 11px +0.6em uppercase overlines. Font bundling (google_fonts or assets) is wired in feature 01.
- **Spacing**: 8-pt grid (`GriotSpacing`); card padding 20 (9, 11); 44px min touch targets everywhere.
- **Radius**: 10 inputs · 16 fields/rows · 20 dialogs · 24 cards/sheets/CTA (`GriotRadii`).
- **Patterns**: segmented stat tiles, striped/hatched meters, stepper rows (−/%/+), calendar phase pills, list tiles with 40px round brand chips (12), blue text-link actions, bottom pill nav (white, radius 24).

Rules: no raw colors/spacing outside tokens; status picker (never drag-drop); every async surface ships empty/loading/error states; parity is feature-complete, not pixel-identical, to web.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
