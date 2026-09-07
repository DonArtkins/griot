# Web Code Standards

- TypeScript strict; no `any`; feature-scoped types + hooks.
- React 18 function components; hooks first; no class components.
- One shell per route subtree: Public = spectacle; App = speed.
- No server data in Zustand; no `localStorage` tokens.
- GSAP only in Public; `useGSAP()` for StrictMode safety.
- Every interactive component has a Jest+RTL test (behavior not snapshots).
- MUI via tokens only; components from the registry.
- `npm run lint/typecheck/test/build` green before "done".

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
