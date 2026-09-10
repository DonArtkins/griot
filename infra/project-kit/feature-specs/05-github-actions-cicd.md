# Feature 05 - GitHub Actions CI/CD

## Type

NEW FEATURE

## What This Delivers

The bootcamp deliverable "Implement GitHub Actions CI/CD": the test-gate pipeline (dotnet, web, mobile, ai, mcp, Newman, Cypress) plus the main-branch deploy job (Vercel, Railway, Trigger, APK artifact).

## Dependencies

- qa suites runnable. Features 01-04 (deploy targets exist).

## Context To Read First

- `research/week-05-deployment-devops.md` sec 4
- `infra/project-kit/context/environment.md`

## Agent Skills To Use

- `infra/.agents/skills/github-actions/SKILL.md`

## Files Owned

- `.github/workflows/ci-cd.yml`
- `.github/workflows/README.md`

## Files

CREATE: workflow with jobs `test-dotnet`, `test-web`, `test-mobile`, `test-ai`, `test-mcp`, `newman`, `cypress`, `deploy` (needs all; `if: github.ref == 'refs/heads/main'`). SQL Server 2022 as a CI service container. Secrets documented in `README.md`: `RAILWAY_TOKEN`, `VERCEL_TOKEN`, `TRIGGER_API_KEY`, `GRIOT_SERVICE_TOKEN`, coverage token. Backend Railway env (spec 06 env sync) additionally carries `TRIGGER_SECRET_KEY` + `WEBHOOK_SECRET` (server-to-server only — never exposed to Vercel/mobile bundles; orchestration contract `research/ai-integration.md` §2a).

## Setup / Initialization

Add repo secrets in GitHub UI; push workflow; verify a PR runs all jobs.

## Implementation Notes

- Hermetic CI: service containers only, never a developer's local compose.
- One concern per job; tests never mixed with deploys; least-privilege secrets.

## Separation of Concerns

- Jobs mirror systems: one per app.

## Docker & Deploy

- CI uses ephemeral containers; deploys trigger real hosts.

## Out of Scope

Staging promotion (v2).

## Acceptance Criteria

- [ ] PRs blocked on any red job; main deploys all apps
- [ ] Mobile APK artifact downloadable


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
