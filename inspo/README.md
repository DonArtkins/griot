# UI Inspiration (inspo/)

Screenshots + references of UI/UX we **desire** — used when building the web frontend and mobile app. The Foundrie pattern: agents MUST consult `inspo/` before building UI, alongside the `examples/` folders and the design-system docs.

## How to use

1. Drop reference screenshots/designs into this folder (`inspo/`), named `<topic>-<source>.png`.
2. Reference them from feature specs / agent prompts: "match the aesthetic of `inspo/board-x.png`".
3. The web/mobile agents read `inspo/` + `project-kit/diagrams/ui/` + `ui-tokens.md` before coding UI.

## Categories you may fill

- `board-*` — kanban board aesthetics (columns, drag, cards)
- `dashboard-*` — dashboard layouts
- `task-modal-*` — task detail modals
- `notifications-*` — notification feeds/bells
- `landing-*` — Awwwards-grade landing pages
- `pricing-*` — pricing pages
- `dark-theme-*` — dark workspace vibes
- `mobile-*` — Flutter/Android patterns (bottom nav, pickers)

## Current content

- `1.jpeg` … `12.jpeg` — the founding reference set (9 web analytics/workspace dashboards + 3 mobile health/fintech apps). Fully cataloged, ranked, and synthesized into the **Griot master design system**: `docs/design/MASTER-DESIGN-SYSTEM.md` (tokens → `project-kit/context/ui-tokens.md` → `web/src/theme.ts` + `mobile/lib/core/theme/theme.dart`).

| File | Surface | What Griot borrows |
|---|---|---|
| `1.jpeg` | Web · market dashboard | Chrome topbar/CTA, pills, segmented chips, tabular density |
| `2.jpeg` | Web · token sheet | Canonical palette + border/ink ramps (verbatim) |
| `3.jpeg` | Web · widget grid | KPI-card anatomy, bento grid, series palette |
| `4.jpeg` | Web · speed insights | **Base structure**: overlines, striped meters, row tables |
| `5.jpeg` | Web · sales dashboard | Nav tree + badges, filter toolbar, KPI meta rows |
| `6.jpeg` | Web · social analytics | Violet/pink series, compare band tables |
| `7.jpeg` | Web · AI usage | KPI meters, gradient bars, usage table |
| `8.jpeg` | Web · workload analytics | Icon dock, overline labels, heatmap/donut grammar |
| `9.jpeg` | Mobile · fitness | **Mobile base**: header tint, 24px cards, pill nav, striped meters |
| `10.jpeg` | Mobile · EV charging | Gradient icon chips + chrome button only (neumorphism rejected) |
| `11.jpeg` | Mobile · cycle health | Stepper rows, calendar phase pills, zone chips |
| `12.jpeg` | Mobile · wallet | List tiles + round brand chips, blue text-link actions |

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**