---
name: vite-react-setup
description: "Scaffold the Griot web app with Vite 5 + React 18 + TypeScript and keep it healthy: npm scripts, env, build, lint, tests."
metadata:
  version: "0.1.0"
---

# Vite + React Setup Skill

## Scaffold

```bash
npm create vite@latest web -- --template react-ts
cd web && nvm use   # .nvmrc -> 20
```

## Env

- `VITE_API_URL` (dev: `http://localhost:PORT`, prod: deployed API base).
- Never prefix other envs with `VITE_` unless public.

## Quality

- Scripts: `lint`, `typecheck`, `test` (Jest+RTL), `build`, `preview`.
- `npm run build` must pass with `tsc -b`; `dist/` is what Vercel serves.
- Dev proxy: `/api` + `/graphql` -> backend in `vite.config.ts`.

## Rules

- No `any`; strict TS. Feature folders own their hooks/types/components.
- No data-crawling into components beyond the registry.
