# Feature 01 — Repo Layout & Separation of Concerns

## Type

NEW FEATURE

## What This Delivers

The Griot monorepo skeleton: `backend/`, `web/`, `mobile/`, `ai/`, `mcp/` folders each with a README and per-app version pins, root `.gitignore`/`.env.example`/`README.md`, and the `PROMPTS/` + `project-kit/` documentation structure. This is the home every later feature drops into — the folder contract is the separation-of-concerns guarantee.

## Dependencies

- None. This is the first feature.
- Approved stack contract from `research/gtp-2026-prep.md` §2 (bootcamp-exact + `[own-stack]` markers).

## Context To Read First

- `AGENTS.md`
- `context/project-overview.md`
- `context/architecture-context.md`
- `context/code-standards.md`

## Files Owned

- `.gitignore`, `.env.example`, `README.md`
- `backend/`, `web/`, `mobile/`, `ai/`, `mcp/` placeholders + READMEs
- `PROMPTS/**`, `project-kit/**`

## Files

CREATE: Root `.gitignore` — env, `node_modules/`, `dist/`, `build/`, `coverage/`, `.trigger/`, OS/editor files.
CREATE: `.env.example` — the documented env variable shapes for backend/web/ai/mcp (see `architecture-context.md`).
CREATE: `README.md` — project intro, repo map, how to run each app, pointer to `AGENTS.md`.
CREATE: Per-app placeholder READMEs (`backend/`, `web/`, `mobile/`, `ai/`, `mcp/`) stating stack + status.
CREATE: Root `docker-compose.yml` stub (services declared in Feature 02).
CREATE: `PROMPTS/` (week-grouped) and `project-kit/` (this kit) committed.

## Setup / Initialization

```bash
cd ~/gtp && mkdir -p griot && cd griot
git init && gh repo create griot --private --source=. --push   # GitHub + branch protection on main
touch .gitignore .env.example README.md
mkdir -p backend web mobile ai mcp .github/workflows PROMPTS project-kit
```

Each app's own scaffold/`init` commands live in **its own** feature spec (Feature 03 → backend, 05 → web, 06 → mobile, 07 → ai/mcp), never here.

## Separation of Concerns

- **Folders are boundaries**: `backend/` (APIs), `web/` + `mobile/` (clients), `ai/` + `mcp/` (intelligence), `PROMPTS/` (design prompts), `project-kit/` (live docs), `research/` (bootcamp research).
- Each Node project (`web/`, `ai/`, `mcp/`) carries its own `.nvmrc` (→ 20) and lockfile — nothing shares a root package.json.
- No app may reach into another app's folder; shared contracts travel through API schemas/DTOs only.

## Docker & Deploy

- No runtime deployment here. The compose file is stubbed for Feature 02; per-app Dockerfiles ship with their owning specs.
- This feature verifies git branch protection is on — the CI/CD (Feature 08) depends on it.

## Out of Scope

- Any app code, schema, containers, or CI jobs.

## Acceptance Criteria

- [ ] Repo structure matches the layout block in `AGENTS.md` exactly
- [ ] `.env.example` documents every variable in `architecture-context.md`'s env table
- [ ] `git status` clean after commit; `main` branch protected on GitHub
- [ ] Each app folder has a README with stack + status

## Future Modifications

- Feature 08 will add real `.github/workflows/` files (placeholders only here).