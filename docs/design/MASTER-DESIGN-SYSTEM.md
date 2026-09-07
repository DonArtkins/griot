# Griot Master Design System

**Status:** Approved visual contract · **Version:** 1.0 · **Date:** 2026-09-07
**Origin:** synthesized from all 12 references in `inspo/` — strongest as base, best pieces borrowed from the rest. Token values live in `project-kit/context/ui-tokens.md`; implementations are `web/src/theme.ts` (MUI v6) and `mobile/lib/core/theme/theme.dart` (Flutter).

**The one-line system:** a light, flat, card-based workspace — white cards with 1px hairlines and pill controls on a cool-gray canvas, dense analytics typography (Inter + Space Grotesk + IBM Plex Mono, tabular numerals), saturated accents reserved exclusively for state/data, dark ink chrome for the primary CTA and dock, and a mobile companion that keeps the same canvas but rounds to 24px with a tinted header.

## 1. Inspo catalog (all 12)

| # | File | Platform / surface | What it shows | Role in synthesis |
|---|---|---|---|---|
| 1 | `inspo/1.jpeg` | Web · crypto market dashboard | Dark topbar + search, filter pills, segmented date chips, tabular money columns, green/red delta labels, calendar strip, "Trade radar" activity list with brand badges | Co-base: chrome topbar, pill/segment grammar, tabular density, activity list |
| 2 | `inspo/2.jpeg` | Web · token sheet + settings nav | Literal design tokens: blue `#3572F6`, text ramp `#282828/#757575/#A1A1A1/#CCCCCC`, borders `#ECECEC/#F7F7F7`, alerts `#47BA39/#F14C43/#0BC1E6/#FF7F1C`, white/`#FAFAFA`/`#EDEDED` bg, spacing annotations | Canonical palette + border/ink ramps (adopted verbatim) |
| 3 | `inspo/3.jpeg` | Web · health/analytics widget grid | KPI cards (icon chip + big value + green delta), bento grid, dotted baselines, multi-series charts (blue/orange/pink/green), white cards, `...` card menus | Co-base: KPI-card anatomy, bento grid, chart series palette |
| 4 | `inspo/4.jpeg` | Web · Vercel-style speed insights | Section overlines, striped bar meters, route tables with dotted row outlines, metric columns, disciplined two-panel layout, orange/green/red data colors | **Base structure**: strongest layout grammar, tables, meters, overlines |
| 5 | `inspo/5.jpeg` | Web · e-commerce sales dashboard | Sub-nested nav tree, count badges, mono/large numerals, blue/orange chart pair, right-rail categories, dark-mode toggle row, filter toolbar | Nav tree + badges, filter toolbar, KPI meta rows, numerals |
| 6 | `inspo/6.jpeg` | Web · social analytics | Violet/pink series colors, compare-column tables, "Agency vs You" tinted header bands, minimal icon rail, export button, date-range chip | Violet/pink secondary series, compare bands, quiet icon rail |
| 7 | `inspo/7.jpeg` | Web · AI usage analytics | Purple/orange/green KPI meters, gradient rounded bars, tooltip + hover highlight, user usage table, "Last updated" meta | KPI meters, gradient bars, hover-highlight charts, usage table |
| 8 | `inspo/8.jpeg` | Web · support workload analytics | Ink icon dock + white active tile, uppercase section labels, heatmap ramp, segmented donut, KPI hero number, orange "Add view" CTA, filter chips row, footer links | Icon rail, overline labels, heatmap/donut grammar, hero KPI |
| 9 | `inspo/9.jpeg` | Mobile · walking/fitness (iOS) | Tinted header gradient, white circular back buttons, rounded-2xl white cards, striped hatched meters, pill bottom nav, segmented stat tiles, smiley slider, full-width purple CTA | **Mobile base**: header tint, card radius, meters, nav, CTA |
| 10 | `inspo/10.jpeg` | Mobile · EV charging (before-state) | Neumorphic emboss, gradient pill buttons, gradient icon chips, segmented progress dots, black "Stop Charging" chrome button | Icon chips + black chrome button only (neumorphism rejected) |
| 11 | `inspo/11.jpeg` | Mobile · sleep/cycle health (iOS) | Same tinted-header family, 3-bar gradient chart, calendar with phase pills, stepper rows (−/%/+), zone color bars, white circular back buttons, purple CTA | Mobile forms: calendar, stepper rows, zone chips, CTA |
| 12 | `inspo/12.jpeg` | Mobile · wallet/fintech (iOS) | Light canvas, list tiles with round brand chips, "Pay Now" blue text links, section rail on right detail, circular action buttons, dark card as hero object | List tiles + round chips, text-link actions, dark hero object |

## 2. Ranking (which inspo leads)

1. **inspo 4 — base (structure & grammar).** Cleanest separation of canvas/cards, overline section labels, striped meters, row tables, and metric columns; every Griot page borrows its skeleton.
2. **inspo 2 — base (palette & tokens).** A real token sheet; its hex values are adopted verbatim for ink, borders, canvas and state colors. Zero guessing.
3. **inspo 1 — co-base (chrome & density).** Dark topbar + "Deposit"-style chrome CTA, filter pills, segmented chips, tabular money columns.
4. **inspo 3 — co-base (cards & charts).** KPI-card anatomy and the multi-series chart palette.
5. **inspo 8 — high borrow.** Icon dock, uppercase labels, heatmap/donut grammar, hero KPI.
6. **inspo 9/11 — mobile base.** Tinted header, 24px cards, meters, steppers, pill nav.
7. **inspo 5/6/7 — targeted borrows.** Nav tree + badges (5), violet/pink series + compare bands (6), meters + usage table (7).
8. **inspo 12 — targeted borrows.** List tiles with round brand chips, text-link actions.
9. **inspo 10 — cautionary + 1 borrow.** Rejects neumorphism/gradient buttons; keeps gradient icon chips and the black chrome button.

## 3. Design principles

1. **Flat-first.** Depth comes from a 1px hairline + position, not shadows; elevation is reserved for things that truly float (popover/modal).
2. **Canvas vs card.** Two surface levels only: `#F7F8FA` canvas, `#FFFFFF` card. Anything else is a state (hover `#FAFAFA`, track `#EDEDED`).
3. **Accents are data.** Blue/violet/pink/info/orange/success appear on state chips, deltas, meters and charts — never as page decoration. The only "brand block" is the dark-chrome CTA/dock.
4. **Type is the interface.** Big tabular numerals for values, overline labels for sections, 13px meta for everything secondary.
5. **Pills everywhere.** Chips, filters, badges and toggles are fully rounded; containers are 16–24px; inputs/buttons 10px.
6. **Mobile = same DNA, softer geometry.** Same canvas and palette, radius steps up to 24, tinted header, white circular icon buttons, bottom pill nav.

## 4. Token system (with per-token provenance)

### 4.1 Canvas & surfaces

| Token | Value | Source | Usage |
|---|---|---|---|
| `color.canvas.base` | `#F7F8FA` | 1, 3, 4, 8 | app canvas, sidebars |
| `color.canvas.raised` | `#FFFFFF` | all | cards, menus, sheets |
| `color.canvas.subtle` | `#FAFAFA` | 2 | row hover, subtle fills |
| `color.canvas.soft` | `#EDEDED` | 2 | segmented track, empty tracks, pressed |
| `color.chrome.ink` | `#1E2022` | 1, 10 (Stop Charging) | dark topbar/dock, primary CTA, toasts |
| `dark.canvas.base` | `#0E0F11` | derived | dark variant canvas |
| `dark.canvas.raised` | `#17181B` | derived | dark variant cards |
| `dark.border` | `rgba(255,255,255,0.08)` | derived | dark variant hairlines |

### 4.2 Ink & borders

| Token | Value | Source | Usage |
|---|---|---|---|
| `color.ink.primary` | `#282828` | 2 | primary text (never pure black) |
| `color.ink.secondary` | `#757575` | 2 | secondary text, meta |
| `color.ink.soft` | `#A1A1A1` | 2 | disabled text, overlines, timestamps |
| `color.ink.inverse` | `#FFFFFF` | 1 | text on chrome |
| `color.border.base` | `#ECECEC` | 2 | card outlines, dividers (always 1px) |
| `color.border.subtle` | `#F7F7F7` | 2 | hairline inner dividers |
| `color.border.inactive` | `#CCCCCC` | 2 | disabled inputs/chips |

### 4.3 Brand, state & data

| Token | Value | Source | Usage |
|---|---|---|---|
| `color.accent.primary` | `#3572F6` | 2 | links, selection, focus, brand fills |
| `color.accent.soft` | `#CEDAF3` | 2 | selected rows, skeleton base |
| `color.accent.wash` | `#FFF7ED` | 2 | hover wash ("blue 50") |
| `color.accent.secondary` | `#7C6CF6` | 6, 7, 9, 11 | secondary series, mobile gradients |
| `color.accent.secondarySoft` | `#EDE9FE` | 6, 7 | secondary soft fills |
| `color.state.success` | `#47BA39` | 2 | == TaskStatus.Done |
| `color.state.warning` | `#FF7F1C` | 2 | == TaskStatus.InReview |
| `color.state.danger` | `#F14C43` | 2 | == Priority.Urgent, overdue |
| `color.state.info` | `#0BC1E6` | 2 | == TaskStatus.InProgress |
| `color.chart.orange` | `#F59E0B` | 1, 5 | == Priority.High, series color |
| `color.chart.pink` | `#EC4899` | 6, 7 | series color, notification dot |
| `color.series` | blue→violet→pink→info→orange→success | 1, 3, 5, 6, 7 | fixed multi-series chart order |
| `color.heatmap` | `#EFF4FF #BFDBFE #93C5FD #60A5FA #3572F6 #1D4ED8` | 8 | density ramps, 0→max |

Soft fills = base color at 12–18% alpha over white (chip bg; text on chip = base color).

### 4.4 Typography (roles)

Inter (UI) + Space Grotesk (display/KPI) + IBM Plex Mono (numerals); metric values always `tabular-nums`.

| Role | Spec | Source |
|---|---|---|
| `type.display` | Space Grotesk 28/34 · 700 · −0.5px | 1 |
| `type.headline` | Space Grotesk 22/28 · 700 · −0.3px | 1, 8 |
| `type.subhead` | Inter 16/22 · 600 | 3, 5 |
| `type.body` | Inter 14/20 · 400 | all |
| `type.body.small` | Inter 13/18 · 400–500 | 1, 4 |
| `type.caption` | Inter 12/16 · 400 · ink.secondary | all |
| `type.overline` | Inter 11/16 · 600 · +0.6em uppercase · ink.soft | 4, 8 |
| `type.kpi` | Space Grotesk 24/30 · 700 · tabular | 3, 5, 6 |
| `type.kpi.hero` | Space Grotesk 32/38 · 700 · tabular | 8 |
| `type.mono` | IBM Plex Mono 13/18 · 500 | 1, 5 |

### 4.5 Spacing, radius, elevation, motion

- **Spacing (8-pt):** xs=4, sm=8, md=12, lg=16, xl=24, 2xl=32, 3xl=48; card padding lg=16 web / xl=20 mobile; grid gaps dense=8 (4) / default=16 (3, 5) / loose=24; kanban column 272–320px; rows 44px.
- **Radius:** sm=10 buttons/inputs (1, 4) · pill=999 chips/badges (1, 3, 4, 8) · md=16 cards/popovers (3, 5, 8) · lg=20 modals (derived) · xl=24 mobile cards/sheets (9, 10, 11). Borders always 1px.
- **Elevation:** E0 none (border-only cards, 4, 8) · E1 `0 1px 2px rgba(16,24,32,0.04)` hover · E2 `0 8px 24px rgba(16,24,32,0.08)`+border popover · E3 `0 24px 64px rgba(16,24,32,0.16)` modal. Neutral shadows only.
- **Motion:** fast=150ms `cubic-bezier(0.2,0,0,1)` app shell · medium=250ms overlays · slow=300–500ms + GSAP public shell only · hover = tint not translate; drag scale 1.02 + E2; skeleton shimmer 1.2s.

## 5. Component rules (canonical patterns, each tagged with source)

1. **Primary CTA — chrome ink** (1, 10): `#1E2022` bg, white label, radius 10; brand blue never used for the top CTA. Filter pills white+border; selected pill = `accent.soft` + blue text.
2. **Segmented control** (1, 4, 8, 10): `canvas.soft` track, radius pill; active segment = white + E1 shadow, weight 600.
3. **KPI card** (3, 5, 6): 32–36px icon chip (soft tint, radius 10) + overline label + `type.kpi` tabular value + soft delta pill (▲ success / ▼ danger) + label:value meta rows in 13px.
4. **Striped meter** (4): 8px tall, radius 4, `repeating-linear-gradient(45deg, c 0 3px, transparent 3px 6px)` in the state color; used for scores/distributions.
5. **Progress track** (7, 8, 9, 11): 8px `canvas.soft` track, radius pill; fill = state color or blue→violet gradient.
6. **Row/table** (4, 6): 44px rows, `canvas.subtle` hover, right-aligned tabular numerals, dotted hairline row separators, chevron icon-chip affordance.
7. **Sidebar** (4, 5): 248px on canvas base; overline section labels; 36px nav items radius 8; active = white card + E1 + weight 600; count badges as `canvas.soft` pills.
8. **Icon rail** (8): 56px `chrome.ink` dock, white icons; active = white rounded-10 tile with ink icon.
9. **Heatmap / donut / calendar** (8, 1): 6-step blue ramp; donut ring 3px segmented with 4° gaps; calendar day cell 36px radius 12, today = success-soft pill.
10. **Compare band tables** (6): two-column compare with tinted header bands (`accent.soft` vs `secondarySoft`).
11. **Toasts** (1, 10): `chrome.ink` bg, white text, radius 12, E2 shadow — never colored backgrounds.
12. **Mobile patterns** (9, 10, 11, 12): header tint gradient `#E4E6F7→canvas.base`; white 44px circular back/close buttons; cards radius 24; stat tiles as segmented mini-cards; stepper rows with −/%/+ chips; calendar phase pills; list tiles with 40px round brand chips; text-link actions in blue; full-width CTA radius 24 (blue on mobile); bottom pill nav.

## 6. Explicitly rejected

- **Neumorphic embossing + gradient pill buttons** (10) — only its gradient icon chips survive.
- **Dark-canvas dashboards** — every inspo is light-canvas; dark exists only as the derived variant and the chrome CTA/dock.
- **Pure black text, colored shadows, gradient borders, double outlines.**

## 7. Implementation map

- **Web:** `web/src/theme.ts` — MUI v6 `createTheme` from `ui-tokens.md`; component overriders implement the §5 patterns (pill chips, 10px buttons, hairline cards at radius 16); fonts via `@fontsource` (Inter, Space Grotesk, IBM Plex Mono).
- **Mobile:** `mobile/lib/core/theme/theme.dart` — Flutter `ThemeData` + `ThemeExtension<GriotColors>` exposing the full token set (Flutter's `ColorScheme` can't carry every token); fonts via `google_fonts`.
- **Contract-sync:** any token change updates `inspo/` → this doc → `ui-tokens.md` → both theme files in the same branch.
