# Web Feature Spec 12 — AI Workspace (Dedicated AI Sidebar + Charts + Report Composition) [own-stack]

**Status:** PLANNED — the frontend half of the agentic-BI research: a full-height persistent **sidebar/panel** (Notion AI / Cursor standard), NOT a floating chat bubble. Renders ai 09's answers, memory threads, capability manifest, and ai 10's SuperAdmin confirm/cancel cards. `inspo/` + design-system tokens throughout; award-grade bar (AGENTS rule 9).

## What This Delivers

- **Persistent AI workspace** — its own route/state (e.g. `/app/ai`), collapsible rail, resizable; conversation threads list with resume; keyboard-first.
- **Rendered answers, not raw text** — typed chart payloads from ai 09 render inline (bars/velocity sparklines from report data; deterministic — the AI emits a chart spec, the client renders); tables/cards from structured content; citations link to source rows.
- **Report composition** — "add to report" collects answers/charts into a draft composition; "export PDF/CSV" / "embed" (share link) ride backend 24; drafts live in the thread until approved.
- **Capabilities-aware** — "What can you do?" panel mirrors `GET /api/me/capabilities` (backend 25); tool buttons that are absent from the manifest are hidden, so the UI can't escalate either.
- **Memory visible** — thread context shows what the Copilot is "remembering" (prior-turn snippets + workspace facts, backend 26) with a one-click forget (delete thread).
- **SuperAdmin ops console (ai 10)** — alert feed (autonomous incident summaries), broadcast compose → preview with recipient count → **confirm/cancel card** (one click, not retyping), and a per-broadcast status list (sent/scheduled/cancelled).

## Dependencies

- web 05/07/10 · ai 09 · ai 10 · backend 20/24/25/26/27. MSW-stubbed copilot in CI.

## Implementation notes (PLANNED)

- `web/src/features/aiWorkspace/**` (panel, threads, chart renderer, composer, ops console); routes `/app/ai` + `/app/ai/threads/:id`.
- Realtime stream (ai 02 pattern) feeds token streaming into the active thread; chart specs arrive as typed JSON chunks.

## Acceptance Criteria (all PENDING)

- [ ] Full-height persistent sidebar opens/resumes a thread; streamed answers render with inline charts from typed specs
- [ ] "Add to report" composes; export/embed produces a shareable artifact (backend 24)
- [ ] Capabilities panel matches the manifest; hidden tools are not rendered and requests to them fail 403 server-side
- [ ] SuperAdmin broadcast: draft → recipient-count preview → confirm/cancel works end-to-end (E2E with MSW stub); non-SuperAdmin sees no ops console
- [ ] Award bar: design-system tokens only, WCAG AA, keyboard-first, reduced-motion, all states covered, Lighthouse ≥ 90, axe clean, `inspo/` Foundrie pass

## Verification

`npm run lint && npm run typecheck && npm test && npm run build`; Cypress E2E with MSW-stubbed copilot + ops console.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.