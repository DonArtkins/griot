# Web Feature Spec 11 — AI Reports & Audit Center (Copilot Superpowers UI) [own-stack]

**Status:** PLANNED — the award-winning presentation layer for the AI superpowers (ai 06/07/08, backend specs 20/24). Sibling: web 10 (Copilot panel). Uses `inspo/` + the Foundrie pattern (AGENTS rule 9) and `web/project-kit/context/design-system.md` tokens throughout.

## What This Delivers

Two connected surfaces inside the App shell:

1. **Reports Center** — gallery of AI/human reports (cards: type, window, creator, generatedAt, PDF/CSV download), detail view with preview + regenerate + delete (Owner/Admin), filters/search per backend 18. Every autonomous Copilot action lands here as an `ai_action_summary` card (ai 08).
2. **Audit Center** — trail explorer over `GET /api/logs/*` (Owner-scoped): entity filters, actor search, severity chips, Before/After problem-diff viewer, CSV export. "Ask the auditor" starts an ai 06 audit that streams findings into the same view.

## The award-winning UX bar (the product story, not decoration)

- **Design:** MUI v6 themed from tokens only; report cards with inline mini-visuals (velocity bars, sparklines); PDFs open in an in-app preview pane; dark/light; responsive; keyboard-first; WCAG AA; reduced-motion respected.
- **Craft:** skeleton loaders everywhere; empty states that teach ("no reports yet — ask the Copilot for your sprint digest"); optimistic interactions; virtualization on long trails; print stylesheet; Lighthouse ≥ 90; axe clean.
- **Performance:** report list/detail cached via TanStack Query (spec 19 cache headers); downloads stream (never full-buffer in JS); trails paginate via spec 18.
- **Copilot integration:** chat → report request → AI plans → user approves (web 10 card) → progress chip in the gallery → complete + notification. Audit answers render as chips/cards, never raw JSON.

## Dependencies

- web 05 (auth) · web 07 (app shell) · web 10 (copilot panel) · backend 16/18/20/24 · ai 06/07/08. MSW-stubbed copilot in CI (no LLM).

## Implementation notes (PLANNED)

- `web/src/features/reports/**` (gallery, detail, preview, download), `web/src/features/audit/**` (explorer, diff viewer, export, auditor ask), shared `copilot` integration hooks.
- Routes `/app/workspaces/:id/reports` + `/app/workspaces/:id/audit`; nav entries in the App shell rail.
- Approval flow (pre-24): report draft card → user approves → web creates the Report row as the user; post-24 the AI may create directly with `CreateReport`.

## Acceptance Criteria (all PENDING)

- [ ] Gallery filters/sorts/derives; PDF/CSV download streams with correct filenames
- [ ] Audit explorer shows Before/After diffs + severity chips; Owner-only data hidden for members
- [ ] Copilot report request → approval card → completion notification → gallery card (E2E with MSW stub)
- [ ] Design review against `design-system.md` tokens + `inspo/` (Foundrie pass); Lighthouse ≥ 90; axe clean
- [ ] All states covered (empty/loading/error); keyboard + reduced-motion verified

## Verification

`npm run lint && npm run typecheck && npm test && npm run build`; Cypress E2E with MSW-stubbed copilot.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.