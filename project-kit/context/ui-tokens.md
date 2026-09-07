# UI Tokens (shared design language)

Origin: the **Griot master design system**, synthesized from the 12 references in `inspo/` (full catalog, rankings, and per-token provenance in `docs/design/MASTER-DESIGN-SYSTEM.md`). These token names feed the web MUI theme (`web/src/theme.ts`) and the mobile Flutter `ThemeData` (`mobile/lib/core/theme/theme.dart`), and are annotated in Figma so the EF Core enums match the visual model.

**Contract note (v2, supersedes the Week-1 placeholder):** the canvas is **light** — white cards on a cool-gray canvas — because 12/12 inspo references are light-canvas. The dark workspace is a **derived variant** (`mode: dark`), not the default. Every token below lists the inspo file(s) it comes from; keep the values verbatim.

## Color — canvas & surfaces

| Token | Value | MUI / Flutter mapping | Notes | Source |
|---|---|---|---|---|
| `color.canvas.base` | `#F7F8FA` | background.default / scaffoldBackgroundColor | cool-gray app canvas | 1, 3, 4, 8 |
| `color.canvas.raised` | `#FFFFFF` | background.paper / Card | every card, menu, sheet | 1–12 |
| `color.canvas.subtle` | `#FAFAFA` | hover row fill | table & list row hover | 2 |
| `color.canvas.soft` | `#EDEDED` | pressed/track fill | segmented-control track, empty tracks | 2 |
| `color.chrome.ink` | `#1E2022` | dark topbar/dock, primary CTA "chrome" variant, Toast bg | the only dark chrome in the light theme | 1 |
| `dark.canvas.base` | `#0E0F11` | dark variant background.default | derived variant | derived |
| `dark.canvas.raised` | `#17181B` | dark variant paper | derived variant | derived |
| `dark.border` | `rgba(255,255,255,0.08)` | dark variant divider | derived variant | derived |

## Color — ink & border

| Token | Value | MUI / Flutter mapping | Notes | Source |
|---|---|---|---|---|
| `color.ink.primary` | `#282828` | text.primary | near-black, never pure `#000` | 2 |
| `color.ink.secondary` | `#757575` | text.secondary | muted labels, meta | 2 |
| `color.ink.soft` | `#A1A1A1` | text.disabled, overline labels, timestamps | | 2 |
| `color.ink.inverse` | `#FFFFFF` | text on dark chrome | | 1 |
| `color.border.base` | `#ECECEC` | divider, card outline | 1px everywhere | 2 |
| `color.border.subtle` | `#F7F7F7` | hairline inner dividers | | 2 |
| `color.border.inactive` | `#CCCCCC` | disabled inputs/chips | | 2 |

## Color — brand, state & data

| Token | Value | MUI / Flutter mapping | Notes | Source |
|---|---|---|---|---|
| `color.accent.primary` | `#3572F6` | primary | blue: links, active/selection, focus, brand fills; **not** the button CTA (CTA is chrome ink) | 2 |
| `color.accent.soft` | `#CEDAF3` | primary soft fill | selected rows, skeleton base | 2 |
| `color.accent.wash` | `#FFF7ED` | primary hover wash | verbatim from token sheet ("blue 50") | 2 |
| `color.accent.secondary` | `#7C6CF6` | secondary | violet: secondary series, mobile gradients, meter fills | 6, 7, 9, 11 |
| `color.accent.secondarySoft` | `#EDE9FE` | secondary soft fill | | 6, 7 |
| `color.state.success` | `#47BA39` | success | == TaskStatus.Done | 2 |
| `color.state.warning` | `#FF7F1C` | warning | == TaskStatus.InReview | 2 |
| `color.state.danger` | `#F14C43` | error | == Priority.Urgent + overdue | 2 |
| `color.state.info` | `#0BC1E6` | info | == TaskStatus.InProgress | 2 |
| `color.chart.orange` | `#F59E0B` | data series / High priority | == Priority.High ("danger-orange") | 1, 5 |
| `color.chart.pink` | `#EC4899` | data series, notification dot | | 6, 7 |
| `color.series` | blue → violet → pink → info → orange → success | chart palette order | fixed order for all multi-series charts | 1, 3, 5, 6, 7 |
| `color.heatmap` | `#EFF4FF #BFDBFE #93C5FD #60A5FA #3572F6 #1D4ED8` | density ramps | 6-step blue ramp, 0→max | 8 |

Soft fills (`Soft` variants of every state color, ~12–18% alpha of the base over white) are the chip/badge background; text on them is the base color. Hard reference: `inspo/2.jpeg` alert tokens.

## Status & priority severity (visual == enum) — unchanged

TaskStatus: Backlog=neutral (`ink.soft`), Todo=blue (`accent.primary`), InProgress=info, InReview=warning, Done=success. Priority: Low=info, Medium=warning, High=chart.orange, Urgent=danger.

## Typography

| Role | Font | Size/Line | Weight | Tracking | Notes | Source |
|---|---|---|---|---|---|---|
| `type.display` | Space Grotesk | 28/34 | 700 | −0.5px | page hero, public display | 1 |
| `type.headline` | Space Grotesk | 22/28 | 700 | −0.3px | page titles | 1, 8 |
| `type.subhead` | Inter | 16/22 | 600 | 0 | card titles | 3, 5 |
| `type.body` | Inter | 14/20 | 400 | 0 | default | all |
| `type.body.small` | Inter | 13/18 | 400–500 | 0 | list rows, meta | 1, 4 |
| `type.caption` | Inter | 12/16 | 400 | 0 | `ink.secondary` | all |
| `type.overline` | Inter | 11/16 | 600 | +0.6em, uppercase | `ink.soft` — sidebar sections | 4, 8 |
| `type.kpi` | Space Grotesk | 24/30 | 700 | −0.3px, tabular-nums | card metrics | 3, 5, 6 |
| `type.kpi.hero` | Space Grotesk | 32/38 | 700 | −0.5px, tabular-nums | score/total panels | 8 |
| `type.mono` | IBM Plex Mono | 13/18 | 500 | 0 | IDs, codes, axis numerals | 1, 5 |

Base fonts: **Inter** (UI) + **Space Grotesk** (display/KPI) + **IBM Plex Mono** (numerals). Load via `@fontsource` on web; `google_fonts` (or bundled assets) on mobile. All metric values render with `font-variant-numeric: tabular-nums`.

## Spacing (8-pt rhythm)

xs=4, sm=8, md=12, lg=16, xl=24, 2xl=32, 3xl=48. Card padding lg=16 (web) / xl=20 (mobile). Grid gaps: dense=8 (inspo 4), default=16 (inspo 3/5), loose=24. Kanban column min/max 272/320px (unchanged). List/table row height 44.

## Radius

| Token | Value | Applied to | Source |
|---|---|---|---|
| `radius.sm` | 10 | buttons, inputs, menu items | 1, 4 |
| `radius.pill` | 999 | chips, filter pills, badges, toggles | 1, 3, 4, 8 |
| `radius.md` | 16 | cards, columns, popovers | 3, 5, 8 |
| `radius.lg` | 20 | modals, dialogs | derived (md+pill) |
| `radius.xl` | 24 | mobile cards, bottom sheets, hero/empty states | 9, 10, 11 |

Borders are always 1px; no gradient borders, no double outlines.

## Elevation (flat-first)

- **E0 (default):** none — cards are white + `border.base` 1px on canvas (4, 8).
- **E1 (hover):** `0 1px 2px rgba(16,24,32,0.04)` — hover lift for actionable cards.
- **E2 (popover):** `0 8px 24px rgba(16,24,32,0.08)` + 1px border — menus, popovers, toasts.
- **E3 (modal):** `0 24px 64px rgba(16,24,32,0.16)` — dialogs, task modal.

Shadows are always neutral gray — never colored. Dark chrome panels use borders, not shadows.

## Motion

- `motion.fast` = 150ms, `cubic-bezier(0.2, 0, 0, 1)` — app shell (hover, chips, panels).
- `motion.medium` = 250ms — overlays (modals, drawers, snackbars).
- `motion.slow` = 300–500ms + GSAP/Lenis — public shell only (unchanged rule).
- Hover = background/border tint (no translate/scale); dragged card scales 1.02 with E2 shadow; skeletons shimmer 1.2s ease-in-out.

## Component patterns (canonical, with source)

- **Primary CTA = chrome ink** (`#1E2022`, white text, radius 10); brand blue is for links/selection/focus. Filter pills: white + border; selected = `accent.soft` bg + blue text; dark-chrome pill reserved for the single top CTA (1).
- **Segmented control:** `canvas.soft` track, active = white + E1 shadow (1, 4, 8, 10).
- **KPI card:** 32–36 icon chip (soft tint, radius 10) + overline label + `type.kpi` value + soft delta pill (▲ success / ▼ danger) + label:value meta rows (3, 5, 6).
- **Striped meter:** 8px tall, radius 4, `repeating-linear-gradient` stripes 3px/3px in the state color (4).
- **Progress track:** 8px `canvas.soft` track; state-color or blue→violet gradient fill (7, 8, 9, 11).
- **Row/table:** 44px rows, `canvas.subtle` hover, right-aligned tabular numerals, chevron icon-chip affordances (4, 6).
- **Sidebar:** 248px, canvas base, overline section labels, 36px nav items radius 8; active = white card + E1 + weight 600; count badges = `canvas.soft` pills (4, 5).
- **Icon rail:** 56px `chrome.ink`, white icons, active = white rounded-10 tile with ink icon (8).
- **Heatmap/donut/calendar:** 6-step blue ramp; 3px segmented donut rings with 4° gaps; calendar day cell 36px radius 12, today = success-soft (8, 1).
- **Mobile:** header tint `#E4E6F7→canvas.base` gradient; back/close buttons = white 44px circles; cards radius 24; full-width primary CTA radius 24 (brand blue on mobile); bottom pill nav (white, radius 24); settings rows with stepper chips; list tiles with 40px round brand chips (9, 11, 12, 10 icon chips only).

## Rejected (do not use)

Neumorphic embossing and gradient pill buttons from `inspo/10.jpeg` (borrow **only** its gradient icon chips); dark-canvas dashboards (all inspos are light); pure-black text; colored shadows.

Rules: no raw colors/spacing outside tokens; status chips == enum map; token changes propagate `inspo/` → `docs/design/MASTER-DESIGN-SYSTEM.md` → theme files → this doc in one change (contract-sync gate).
