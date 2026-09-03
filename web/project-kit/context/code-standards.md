# Web Code Standards

- TypeScript strict; no `any`; feature-scoped types + hooks.
- React 18 function components; hooks first; no class components.
- One shell per route subtree: Public = spectacle; App = speed.
- No server data in Zustand; no `localStorage` tokens.
- GSAP only in Public; `useGSAP()` for StrictMode safety.
- Every interactive component has a Jest+RTL test (behavior not snapshots).
- MUI via tokens only; components from the registry.
- `npm run lint/typecheck/test/build` green before "done".
