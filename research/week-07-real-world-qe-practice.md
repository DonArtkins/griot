# Week 7 — Real-World Quality Engineering Practice (Project Griot)

> Guide's official Week 7 goals: complete test strategy & plans for the capstone → execute full manual testing cycles → automated suites (≥80% coverage) → log & track defects in Jira with evidence → perform QE on Sababisha Solutions' internal products → **performance benchmarking & UAT** → produce executive test summary reports → integrate automated testing into CI/CD.

---

## 1. Decisions & Rationale

- **The test strategy doc is a living document started in Week 2** (or earlier), not a Week-7 blank page. Week 7 finalizes and formats it for the executive audience.
- **Manual testing runs against the deployed Railway/Vercel system** (not localhost) — the first real validation of the Week-5 pipeline: env-var mismatch, CORS, cold-start, and Railway-specific latency are exactly what automated tests miss.
- **UAT = "would a small team actually run a sprint in this?"** — recruit one or two people, give one simple task ("create a project, add three tasks, move one to Done"), watch where they hesitate, and log friction like defects.
- **QE on Sababina internal products** — cohort-scheduled and out of Griot's direct control; the *approach* transfers (same rigor, same Jira workflows). Budget time explicitly so a perfectionist Griot pass doesn't swallow it.
- The stack is unchanged from Weeks 2–6: .NET 8 backend, React/Vite/MUI frontend, Flutter mobile, all already covered by the Week-6 gate.

## 2. Test Strategy Doc — Outline (readable by non-technical stakeholders)

1. Scope (v1 features from Week 1; deferred list acknowledged)
2. Objectives — core loop works end-to-end; auth sound per Week-6 OWASP; performance meets k6 baseline; no critical/high defects open
3. Test types — manual (this week), unit/integration (xUnit/Jest), E2E (Cypress), API (Newman), perf (k6), accessibility (axe)
4. Environments — local, CI, Railway/Vercel deployed
5. Entry/exit criteria — entered when CI gate is green; exited with zero critical/high open + coverage met + UAT feedback addressed
6. Risk areas — pulled from the masters & weeks 2–6 watch-outs (rotation race, N+1, Railway limits, GSAP flake)
7. Traceability matrix — requirement ↔ test case ↔ defect

## 3. Manual test cycle
Full pass over the two shells: auth flows, project/board/task CRUD, drag-drop, comments, notifications, bulk ops (spot-check the stored-proc path), search/filters, settings/invites, mobile Flutter app (Android emulator) — **plus the AI layer**: Copilot chat, propose-approve mutations, scheduled digest/reminder outputs, and the MCP tools exercised via MCP Inspector. Log every defect to Jira with repro steps, severity, and a screenshot/video.

## 4. Automated suites (already in CI from Week 5/6)
`dotnet test` (xUnit, incl. refresh-rotation replay), Jest + RTL, Cypress, Newman collection (REST + GraphQL), `flutter test`. Confirm every job is embedded in the GitHub Actions workflow and gate merges on `main`.

## 5. Performance benchmarking
Re-run k6 against the deployed API (dashboard query, login, board read). Record baseline + any regression vs Week 6. Feed into the executive summary as pp/vs/observations.

## 6. UAT script
- One person, one task: "Create a project, add three tasks, and move one to Done — no other instruction."
- Second persona: "Ask Griot what's blocked on your project" on the Copilot panel — is the streaming answer genuinely useful, and does it propose sensible taggable actions?
- Watch/ask them to narrate; log hesitation and mis-clicks as friction findings even when they aren't bugs.

## 7. Executive Test Summary Report (1–2 pages)
- Verdict: ship-ready / ship-ready with known issues / not ready
- Pass/fail by area (auth, core flow, notifications, performance, accessibility)
- Top 3 risks in plain language
- Recommendation (ship / fix-then-ship / defer items)

## 8. Public Launch & Awwwards Readiness
- Meta tags, OG image, favicon set; final Lighthouse run of the deployed Public Shell.
- Awwwards submission treated as a **stretch goal** (fee + high bar). Fallbacks: Site Inspire, Lapa Ninja, or a strong portfolio write-up. The core value is still achieved: a live, documented, tested product.

## 9. Definition of Done — Week 7 (and the programme)
- [ ] Test strategy finalized from a living draft (not from scratch)
- [ ] Manual cycle complete against the deployed system, exploratory included
- [ ] All defects in Jira with evidence; traceability matrix maintained
- [ ] k6 regression benchmark recorded
- [ ] AI layer verified end-to-end: Copilot chat + approve-mutation, digest delivery, MCP `get_board`/`create_task` calls
- [ ] UAT done with ≥1 outsider; friction logged
- [ ] Executive summary written and reviewed
- [ ] QE applied to the internal Sababisha product as assigned
- [ ] Public Shell polish (meta/OG/favicon/Lighthouse) + showcase decision made
- [ ] Griot documented and added to the portfolio

---
**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
**Griot — the record of what the team built, and how well they built it.**