# UI Rules (both shells)

1. **Two shells, two budgets.** Public = spectacle + conversion (GSAP/Lenis, soft shadows, display type). App shell = speed + clarity (MUI transitions, `motion.fast`, no decorative animation). GSAP never enters the app shell.
2. **Board drag-drop is web-only.** Mobile uses a status picker.
3. **Propose-before-write for AI.** Copilot streams answers; mutations return as approval cards; the app performs the write after approval.
4. **Status colors are the enum.** Chips use the severity map in `ui-tokens.md`.
5. **Empty/loading/error states are designed assets** everywhere; never a bare grey rectangle.
6. **Accessibility is a gate.** 44x44 min touch, visible focus, WCAG AA, axe-clean on the Public shell.
7. **Layout.** App shell: fixed left rail + header, only content scrolls; board columns scroll horizontally; responsive 1/2/3-col grids. Public shell: hero shows real product UI; sticky minimal nav; trust signals at decision points.
8. **Server data lives in query caches** (Apollo / TanStack / graphql_flutter); client stores hold only client-only state.
9. **Optimistic updates** for column moves / status changes; rollback + toast + refetch on failure.
10. **Auth UX.** 401 -> silent refresh -> retry once; refresh failure clears session -> /login. No infinite retry loops.
