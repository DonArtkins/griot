# Feature 01 - Frontend Deployment (Vercel)

## Type

NEW FEATURE

## What This Delivers

The bootcamp Week-5 steps 1-5 "Frontend Deployment (Vercel)": the web app live on Vercel using the Vite framework preset, env-configured, and verified.

## Dependencies

- Web feature 01 (buildable app).
- GitHub repo.

## Context To Read First

- `infra/project-kit/context/{deployment-targets,environment}.md`

## Agent Skills To Use

- `infra/.agents/skills/vercel-deploy/SKILL.md`

## Files Owned

- `infra/project-kit/context/deployment-targets.md` (web row)

## Setup / Initialization

1. Push `web/` to GitHub.
2. Vercel: New Project, Import repo.
3. Framework preset: **Vite**.
4. Env: `VITE_API_URL` = deployed API base.
5. Deploy and verify the live URL.

## Implementation Notes

- Assets from `dist`; preview deployments per PR; production on `main`.
- Record the Lighthouse budget baseline (qa re-measures).

## Separation of Concerns

- Vercel project config lives here (infra); code stays in `web/`. No infra code inside web beyond `vercel.json` overrides if needed.

## Docker & Deploy

- This IS the deploy.

## Out of Scope

Backend containers (02), compose (03), CI (05).

## Acceptance Criteria

- [ ] Live URL serves the Vite build; env points at the deployed API
- [ ] Assets load; Lighthouse captured


## Multi-Tenant Update (2026-09-11 — PLANNED)

- Org-aware deployment checks: the post-deploy smoke extends to the JWT v2 login flow (org claim present for multi-org users, `select-organization` reachable) against the deployed API.
- Env unchanged (`VITE_API_URL` only); no tenant data is cached in the Vercel edge or static bundle — org scope is always server-resolved (backend 29/30).
- No Vercel project setting change; contract-sync only when backend 29/30 land.

---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
