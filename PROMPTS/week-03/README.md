# Week 03 — Frontend Prompts

**Week goal (from the roadmap):** React app setup with Vite → MUI v6 integration → REST integration (Axios + TanStack Query) → GraphQL integration (Apollo Client) → secure authentication & state management.

## Planned prompts

| # | Prompt | Tool | Output |
|---|---|---|---|
| 01 | Web app implementation from the UI registry | Agent (Cline/Claude) | Feature 05: Vite+React+MUI two shells |
| 02 | MUI theme ⇄ Figma token sync | Agent | `web/src/theme.ts` from ui-tokens |
| 03 | Auth wiring + silent refresh | Agent | Auth flow vs the .NET backend |

## Status

Not started — write the prompts when Feature 04 (backend) is green. Draft on the agent prompt below:

> Build **Feature 05** (`feature-specs/05-web-app-frontend.md`). Scaffold `web/` with Vite (React-TS), theme MUI from `ui-tokens.md`, wire Apollo (GraphQL) + Axios/TanStack (REST) to the local .NET API, implement the Public + App shells from `ui-registry.md`, and enforce `ui-rules.md` (drag-drop web-only, empty/loading/error states, no server data in Zustand). Run `npm run lint && npm run typecheck && npm test && npm run build` and fix everything before "done."